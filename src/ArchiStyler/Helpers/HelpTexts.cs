namespace ArchiStyler.Helpers;

public static class HelpTexts
{
    public const string DiagramNavigation =
        "Ctrl + колёсико — масштаб. Папки 📁 — перетаскивание за заголовок. Класс в папку / на корень — обновляет namespace и путь файла при экспорте.";
    public const string AddFolder =
        "Создать папку на диаграмме (Models, Handlers…). Классы внутри экспортируются в соответствующие каталоги.";

    // Стартовое окно
    public const string StartupLanguage =
        "Целевой язык генерации файлов: C# (.cs, .csproj) или Java (.java, pom.xml).";
    public const string StartupLanguageHint =
        "Пример: C# → namespace App.Services; Java → package app.services;";
    public const string StartupProjectName =
        "Имя архитектурного проекта. Используется в имени файла .archistyler.json и в csproj/pom.";
    public const string StartupFolder =
        "Папка, куда будут записаны сгенерированные файлы при экспорте.";
    public const string StartupNamespace =
        "Namespace по умолчанию для новых классов C# (например App.Core).";
    public const string StartupPackage =
        "Package по умолчанию для новых классов Java (например app.core).";

    // Левая панель
    public const string AddClass =
        "Добавить пустой класс на холст. Перетащите карточку мышью для расположения.";
    public const string ClearDiagram =
        "Удалить все классы, связи и очистить превью кода. Файлы на диске не удаляются.";
    public const string LinkKind =
        "Тип по умолчанию (при создании связи откроется выбор с учётом классов).";
    public const string LinkKindHint =
        "После соединения точек: наследование, реализация, поле, метод, using/import и др.";
    public const string RoleCombo =
        "Роль класса в архитектуре (DTO, Service, DAO…). Добавляет типовые поля и методы.";
    public const string AddByRole =
        "Создать класс с заготовкой под выбранную роль.";
    public const string PatternCombo =
        "Готовый шаблон паттерна (MVP, MVVM, Singleton, GoF и др.).";
    public const string ApplyPattern =
        "Вставить на диаграмму все классы и связи шаблона. Редактируйте patterns.json для своих шаблонов.";

    // Шапка
    public const string ThemeToggle = "Переключить светлую / тёмную тему интерфейса.";
    public const string SaveProject = "Сохранить диаграмму в файл ИмяПроекта.archistyler.json в папке проекта.";
    public const string Export = "Сгенерировать .cs/.java файлы и .csproj или pom.xml в папку проекта.";
    public const string OpenHelp = "Открыть полную справку ArchiStyler.";

    // Инспектор класса
    public const string ClassName = "Имя типа в коде. Пример: UserService, OrderRepository.";
    public const string ClassNamespace = "Пространство имён C#. Пример: MyApp.Domain.Entities";
    public const string ClassPackage = "Пакет Java. Пример: com.myapp.domain";
    public const string ClassSummary = "Комментарий XML/summary в сгенерированном файле (необязательно).";
    public const string IsInterface = "interface — только контракт, без реализации.";
    public const string IsAbstract = "abstract class — нельзя создать экземпляр, нужны наследники.";
    public const string IsEnum = "enum — перечисление констант.";
    public const string IsSealed = "sealed — запрет наследования (C#).";
    public const string Access = "Модификатор доступа типа: public, internal, …";
    public const string BaseType = "Базовый класс (extends). Пример: BaseEntity. Стрелка наследования на диаграмме.";
    public const string NewUsing = "using (C#) или import (Java). Пример: System.Collections.Generic";
    public const string AddUsing = "Добавить строку в список импортов класса.";
    public const string MemberField = "Приватное или публичное поле.";
    public const string MemberProperty = "Свойство с get/set (C#) или поле + геттер/сеттер (Java).";
    public const string MemberMethod = "Метод с телом-заглушкой при включённой опции.";
    public const string MemberCtor = "Конструктор с именем класса.";
    public const string MemberStubs = "Включить генерацию заглушек для всех методов.";
    public const string MemberKind = "Field, Property, Method или Constructor.";
    public const string MemberName = "Имя члена. Пример: GetById, Save, Title.";
    public const string MemberType = "Тип поля/свойства или возвращаемый тип метода. Пример: string, void, List<Order>.";
    public const string GenerateStub = "В теле метода будет TODO или return по умолчанию.";
    public const string DeleteClass = "Удалить класс с диаграммы и все его связи.";

    // Код
    public const string RefreshCode = "Сгенерировать текст из текущей модели класса.";
    public const string ApplyCode =
        "Разобрать текст в редакторе и обновить модель (имя, члены, using). Эвристический парсер — для сложного кода проверяйте результат.";

    // Связи на диаграмме
    public const string AnchorPoint =
        "Потяните к точке другого класса — откроется выбор типа связи (наследование, поле, метод, using…).";

    public const string FullHelp = """
        # ArchiStyler — справка

        ## Назначение
        ArchiStyler помогает быстро спроектировать архитектуру на C# или Java: UML-подобная диаграмма,
        роли классов, шаблоны паттернов, превью и экспорт файлов в IDE.

        ## Быстрый старт
        1. Выберите язык и папку проекта на стартовом экране.
        2. Нажмите «Применить» у шаблона (MVP, Repository…) или «+ Класс».
        3. Кликните класс — справа отредактируйте свойства и члены.
        4. Соедините классы: потяните с цветной точки на краю карточки к точке другого класса.
        5. Вкладка «Код» — превью; «Экспорт + проект» — файлы на диск.

        ## Холст диаграммы
        - **Масштаб**: Ctrl + колёсико или кнопки + / − / «Сброс» в углу.
        - **Панорама**: средняя кнопка мыши + перетаскивание, либо **пробел** + левая кнопка по пустому месту.
        - **Прокрутка**: полосы прокрутки для больших схем (холст расширяется под содержимое).
        - **Перемещение класса**: перетащите карточку левой кнопкой.
        - **Выбор**: клик по карточке — редактор справа.

        ## Типы связей
        | Тип | Смысл | Пример |
        |-----|--------|--------|
        | Inherits | Наследование | class B : A |
        | Implements | Реализация интерфейса | class S : IService |
        | Uses | Зависимость / использует | поле, параметр |
        | Composes | Сильная композиция | владеет жизненным циклом |
        | Aggregates | Агрегация | слабая связь «часть-целое» |
        | Поле / Метод | Ссылка на член | существующий или новый |
        | using / import | Импорт пространства имён | из целевого класса |

        После соединения точек откроется диалог выбора доступных типов. Стрелку можно перетащить за конец (к другой точке) или удалить (Delete / ПКМ).
        Стрелки также появляются автоматически, если указать **Базовый тип** или интерфейсы в инспекторе.

        ## Роли классов
        Роль (DTO, Service, DAO, Logger…) добавляет типовые члены-заготовки. Это ускоряет прототипирование
        и помогает стандартизировать слои в команде.

        ## Паттерны (JSON)
        Файлы в Assets/Templates/*.json. Каждый паттерн описывает классы, члены и связи.
        Можно добавить свой patterns-custom.json по тому же формату.

        ## Инспектор класса
        - **Имя** — имя типа в файле.
        - **Namespace / Package** — путь пакета (зависит от языка).
        - **interface / abstract / enum / sealed** — вид типа.
        - **Базовый тип** — родительский класс (один).
        - **using/import** — список пространств имён.
        - **Члены** — поля, свойства, методы; «заглушка» — тело по умолчанию.

        ## Вкладка «Код»
        - «Обновить из модели» — сгенерировать код из диаграммы.
        - «Применить код → модель» — обратный разбор (упрощённый, не Roslyn).

        ## Экспорт
        Создаёт структуру папок по namespace/package, файлы классов и csproj/pom.xml.

        ## Сохранение проекта
        Файл .archistyler.json — диаграмма для повторного открытия (функция загрузки — в развитии).

        ## Подсказки
        Наведите курсор на элемент интерфейса — краткая подсказка. Под полями — серый текст с примерами.
        """;
}
