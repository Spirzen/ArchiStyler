using System.Collections.ObjectModel;
using ArchiStyler.Helpers;
using ArchiStyler.Models;
using ArchiStyler.Services;
using ArchiStyler.Services.Parsing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArchiStyler.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly TemplateService _templates = new();
    private readonly ProjectExportService _export = new();

    public ProjectModel Project { get; }

    public event EventHandler? DiagramInvalidated;

    [ObservableProperty] private ClassNodeViewModel? _selectedNode;
    [ObservableProperty] private ClassDefinition? _editingClass;
    [ObservableProperty] private string _codePreview = "";
    [ObservableProperty] private string _statusMessage = "Готово";
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private PatternTemplate? _selectedPattern;
    [ObservableProperty] private ClassRoleItem? _selectedRoleItem;
    [ObservableProperty] private string _newUsing = "";
    [ObservableProperty] private string _themeButtonLabel = "☀ Светлая тема";
    [ObservableProperty] private RelationKind _selectedLinkKind = RelationKind.Uses;
    [ObservableProperty] private LinkKindItem? _selectedLinkKindItem;
    [ObservableProperty] private FolderDefinition? _editingFolder;

    public ObservableCollection<ClassNodeViewModel> Nodes { get; } = [];
    public ObservableCollection<FolderNodeViewModel> FolderNodes { get; } = [];
    public ObservableCollection<LinkKindItem> LinkKindItems { get; } = [];
    public ObservableCollection<PatternTemplate> Patterns { get; } = [];
    public ObservableCollection<ClassRoleItem> RoleItems { get; } = new(RoleCatalog.All);
    public ObservableCollection<AccessModifier> AccessModifiers { get; } = new(Enum.GetValues<AccessModifier>());
    public ObservableCollection<MemberKind> MemberKinds { get; } = new(Enum.GetValues<MemberKind>());

    public string ProjectTitle => $"{Project.Name} · {EnumDisplay.FormatLanguage(Project.Language)} · {Project.RootPath}";
    public bool IsCSharp => Project.Language == TargetLanguage.CSharp;

    public MainViewModel(ProjectModel project)
    {
        Project = project;
        foreach (var kind in Enum.GetValues<RelationKind>())
            LinkKindItems.Add(new LinkKindItem(kind, RelationKindHelper.DisplayName(kind, project.Language)));
        SelectedLinkKindItem = LinkKindItems.FirstOrDefault(i => i.Kind == RelationKind.Uses)
                               ?? LinkKindItems.FirstOrDefault();
        SelectedRoleItem = RoleItems.FirstOrDefault(r => r.Role == ClassRole.Service);
        UpdateThemeLabel();
        LoadPatterns();
    }

    partial void OnSelectedLinkKindItemChanged(LinkKindItem? value)
    {
        if (value is not null)
            SelectedLinkKind = value.Kind;
    }

    private void LoadPatterns()
    {
        Patterns.Clear();
        foreach (var p in _templates.LoadPatterns().Patterns)
            Patterns.Add(p);
        SelectedPattern = Patterns.FirstOrDefault();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeService.Toggle();
        UpdateThemeLabel();
        StatusMessage = ThemeService.Current == AppTheme.Light ? "Светлая тема" : "Тёмная тема";
    }

    private void UpdateThemeLabel() =>
        ThemeButtonLabel = ThemeService.Current == AppTheme.Dark ? "☀ Светлая тема" : "🌙 Тёмная тема";

    [RelayCommand]
    private void ClearDiagram()
    {
        Project.Classes.Clear();
        Project.Relations.Clear();
        Project.Folders.Clear();
        Nodes.Clear();
        FolderNodes.Clear();
        EditingClass = null;
        EditingFolder = null;
        SelectedNode = null;
        CodePreview = "";
        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
        StatusMessage = "Диаграмма и код очищены";
    }

    [RelayCommand]
    private void AddFolder()
    {
        var defaults = new[] { "Models", "Data", "Handlers", "Services", "Views", "Core" };
        var name = defaults[Project.Folders.Count % defaults.Length];
        var folder = new FolderDefinition
        {
            Name = name,
            Segment = name,
            X = 48 + Project.Folders.Count * 36,
            Y = 48 + Project.Folders.Count * 28,
            Width = 340,
            Height = 260
        };
        Project.Folders.Add(folder);
        RefreshNodes();
        SelectFolder(folder);
        StatusMessage = $"Папка «{name}» — перетащите классы внутрь";
    }

    [RelayCommand]
    private void DeleteSelectedFolder()
    {
        if (EditingFolder is null) return;
        foreach (var cls in Project.Classes.Where(c => c.FolderId == EditingFolder.Id).ToList())
            ProjectPathHelper.ApplyFolderAssignment(cls, Project, null);
        Project.Folders.Remove(EditingFolder);
        EditingFolder = null;
        RefreshNodes();
        StatusMessage = "Папка удалена; классы перенесены в корень";
    }

    [RelayCommand]
    private void AddClass()
    {
        var offset = Nodes.Count * 24;
        var (x, y) = GetPlacementForNewClass(offset);
        var cls = _templates.CreateFromRole(ClassRole.None, Project, x, y);
        if (EditingFolder is not null)
            ProjectPathHelper.ApplyFolderAssignment(cls, Project, EditingFolder.Id);
        AddClassInternal(cls);
        SelectClass(cls);
        StatusMessage = $"Добавлен класс {cls.Name}";
    }

    [RelayCommand]
    private void AddClassByRole()
    {
        var role = SelectedRoleItem?.Role ?? ClassRole.None;
        var offset = Nodes.Count * 20;
        var cls = _templates.CreateFromRole(role, Project, 80 + offset, 80 + offset);
        AddClassInternal(cls);
        SelectClass(cls);
        StatusMessage = $"Добавлен: {SelectedRoleItem?.DisplayName ?? role.ToString()}";
    }

    [RelayCommand]
    private void ApplyPattern()
    {
        if (SelectedPattern is null) return;
        var ox = 40 + Nodes.Count * 12;
        _templates.ApplyPattern(Project, SelectedPattern, ox, ox);
        RefreshNodes();
        StatusMessage = $"Шаблон «{SelectedPattern.Name}» применён — выберите класс на диаграмме";
    }

    [RelayCommand]
    private void AddMemberField() => AddMember(MemberKind.Field, "field", "string");

    [RelayCommand]
    private void AddMemberProperty() => AddMember(MemberKind.Property, "Property", "string");

    [RelayCommand]
    private void AddMemberMethod() => AddMember(MemberKind.Method, "DoWork", "void", isMethod: true);

    [RelayCommand]
    private void AddConstructor()
    {
        if (EditingClass is null) return;
        EditingClass.Members.Add(new MemberDefinition
        {
            Kind = MemberKind.Constructor,
            Access = AccessModifier.Public,
            GenerateStub = true
        });
        NotifyClassChanged();
    }

    private void AddMember(MemberKind kind, string name, string type, bool isMethod = false)
    {
        if (EditingClass is null) return;
        EditingClass.Members.Add(new MemberDefinition
        {
            Kind = kind,
            Name = name,
            Type = type,
            ReturnType = isMethod ? type : "void",
            Access = kind == MemberKind.Field ? AccessModifier.Private : AccessModifier.Public,
            GenerateStub = isMethod
        });
        NotifyClassChanged();
    }

    [RelayCommand]
    private void AddStubsForAllMethods()
    {
        if (EditingClass is null) return;
        foreach (var m in EditingClass.Members.Where(x => x.Kind == MemberKind.Method))
            m.GenerateStub = true;
        NotifyClassChanged();
        StatusMessage = "Заглушки включены для всех методов";
    }

    [RelayCommand]
    private void RemoveSelectedMember(MemberDefinition? member)
    {
        if (EditingClass is null || member is null) return;
        EditingClass.Members.Remove(member);
        NotifyClassChanged();
    }

    [RelayCommand]
    private void AddUsing()
    {
        if (EditingClass is null || string.IsNullOrWhiteSpace(NewUsing)) return;
        var u = NewUsing.Trim();
        if (!EditingClass.Usings.Contains(u))
            EditingClass.Usings.Add(u);
        NewUsing = "";
        RefreshCodePreview();
    }

    [RelayCommand]
    private void RemoveUsing(string? usingName)
    {
        if (EditingClass is null || string.IsNullOrWhiteSpace(usingName)) return;
        EditingClass.Usings.Remove(usingName);
        RefreshCodePreview();
    }

    [RelayCommand]
    private void RefreshCodePreview()
    {
        if (EditingClass is null)
        {
            CodePreview = "// Выберите класс на диаграмме";
            return;
        }

        try
        {
            var gen = CodeGeneratorFactory.Create(Project.Language);
            CodePreview = gen.GenerateClass(EditingClass, Project);
        }
        catch (Exception ex)
        {
            CodePreview = $"// Ошибка генерации: {ex.Message}";
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void ApplyCodeFromPreview()
    {
        if (EditingClass is null || string.IsNullOrWhiteSpace(CodePreview))
        {
            StatusMessage = "Нет кода для разбора.";
            return;
        }

        var parser = CodeParserFactory.Create(Project.Language);
        var result = parser.Parse(CodePreview);
        if (!result.Success)
        {
            StatusMessage = $"Разбор: {result.Error}";
            return;
        }

        parser.ApplyToClass(EditingClass, result, Project);
        SyncRelationsFromClass(EditingClass);
        NotifyClassChanged();
        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
        StatusMessage = "Модель обновлена из редактора кода.";
    }

    [RelayCommand]
    private void ExportProject()
    {
        var result = _export.ExportAll(Project, includeScaffold: true);
        if (result.Errors.Count == 0)
            StatusMessage = $"Экспорт: {result.WrittenFiles.Count} файлов (включая проект)";
        else
            StatusMessage = $"Готово с замечаниями: {string.Join("; ", result.Errors)}";
    }

    [RelayCommand]
    private void SaveProject()
    {
        var path = Path.Combine(Project.RootPath, $"{Project.Name}.archistyler.json");
        _export.SaveProject(Project, path);
        StatusMessage = $"Проект сохранён: {path}";
    }

    [RelayCommand]
    private void DeleteSelectedClass()
    {
        if (EditingClass is null) return;
        Project.Classes.Remove(EditingClass);
        Project.Relations.RemoveAll(r => r.FromClassId == EditingClass.Id || r.ToClassId == EditingClass.Id);
        RefreshNodes();
        EditingClass = null;
        SelectedNode = null;
        CodePreview = "";
        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
        StatusMessage = "Класс удалён";
    }

    public void SelectClass(ClassDefinition cls)
    {
        SelectFolder(null);
        foreach (var n in Nodes)
            n.IsSelected = n.Id == cls.Id;

        SelectedNode = Nodes.FirstOrDefault(n => n.Id == cls.Id);
        EditingClass = cls;
    }

    public void SelectFolder(FolderDefinition? folder)
    {
        EditingFolder = folder;
        if (folder is not null)
        {
            EditingClass = null;
            SelectedNode = null;
            CodePreview = "// Выберите класс в папке";
        }

        foreach (var f in FolderNodes)
            f.IsSelected = folder is not null && f.Id == folder.Id;
    }

    public void DeselectAll()
    {
        SelectFolder(null);
        EditingClass = null;
        SelectedNode = null;
        foreach (var n in Nodes)
            n.IsSelected = false;
    }

    public void OnDiagramChanged()
    {
        SelectedNode?.SyncFromModel();
        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void SelectNode(ClassNodeViewModel? node)
    {
        if (node is null) return;
        node.SyncToModel();
        SelectClass(node.Model);
    }

    public void AddClassInternal(ClassDefinition cls)
    {
        Project.Classes.Add(cls);
        var vm = new ClassNodeViewModel(cls, Project);
        vm.SyncFromModel();
        Nodes.Add(vm);
        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshNodes()
    {
        var selectedId = EditingClass?.Id;
        var selectedFolderId = EditingFolder?.Id;
        Nodes.Clear();
        FolderNodes.Clear();
        foreach (var folder in Project.Folders)
        {
            var fvm = new FolderNodeViewModel(folder, Project);
            fvm.SyncFromModel();
            FolderNodes.Add(fvm);
        }
        foreach (var cls in Project.Classes)
        {
            var vm = new ClassNodeViewModel(cls, Project);
            vm.SyncFromModel();
            Nodes.Add(vm);
        }

        if (selectedId is not null)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == selectedId);
            if (node is not null)
            {
                EditingClass = node.Model;
                SelectedNode = node;
                foreach (var n in Nodes)
                    n.IsSelected = n.Id == selectedId;
            }
            else
            {
                EditingClass = null;
                SelectedNode = null;
            }
        }
        else if (selectedFolderId is not null)
        {
            var folder = Project.Folders.FirstOrDefault(f => f.Id == selectedFolderId);
            if (folder is not null)
                SelectFolder(folder);
            else
                EditingFolder = null;
        }

        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
    }

    private (double x, double y) GetPlacementForNewClass(int offset)
    {
        if (EditingFolder is null)
            return (60 + offset, 60 + offset);
        return (EditingFolder.X + 24 + offset % 80, EditingFolder.Y + 48 + offset % 60);
    }

    partial void OnEditingFolderChanged(FolderDefinition? value)
    {
        if (value is null) return;
        if (!string.IsNullOrWhiteSpace(value.Name) && string.IsNullOrWhiteSpace(value.Segment))
            value.Segment = value.Name;
        ProjectPathHelper.SyncAllClassesInFolder(Project, value.Id);
        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
        if (EditingClass is not null)
            RefreshCodePreview();
    }

    private void SyncRelationsFromClass(ClassDefinition cls)
    {
        Project.Relations.RemoveAll(r => r.FromClassId == cls.Id);

        void AddRelation(string targetName, RelationKind kind)
        {
            var target = Project.Classes.FirstOrDefault(c =>
                c.Name.Equals(targetName, StringComparison.Ordinal));
            if (target is null) return;
            Project.Relations.Add(new RelationDefinition
            {
                FromClassId = cls.Id,
                ToClassId = target.Id,
                Kind = kind
            });
        }

        if (!string.IsNullOrWhiteSpace(cls.BaseType))
            AddRelation(cls.BaseType, RelationKind.Inherits);
        foreach (var iface in cls.ImplementedInterfaces)
            AddRelation(iface, RelationKind.Implements);
    }

    private void NotifyClassChanged()
    {
        SelectedNode?.SyncFromModel();
        OnPropertyChanged(nameof(EditingClass));
        RefreshCodePreview();
        DiagramInvalidated?.Invoke(this, EventArgs.Empty);
    }

    partial void OnEditingClassChanged(ClassDefinition? value)
    {
        if (value is not null)
        {
            if (Project.Language == TargetLanguage.CSharp)
                value.Namespace = ProjectPathHelper.GetEffectiveNamespace(value, Project);
            else
                value.Package = ProjectPathHelper.GetEffectivePackage(value, Project);
        }
        RefreshCodePreview();
    }
}
