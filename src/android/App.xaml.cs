﻿using Microsoft.Maui.Controls;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает функционал приложения
	/// </summary>
	public partial class App: Application
		{
		#region Общие переменные и константы

		// Флаги прав доступа
		private RDAppStartupFlags flags;

		// Главный журнал приложения
		private List<MainLogItem> masterLog;

		// Управление центральной кнопкой журнала
		private bool centerButtonEnabled = true;
		private const string semaphoreOn = "●";
		private const string semaphoreOff = "○";

		// Параметры прокрутки журнала
		private bool needsScroll = true;
		private int currentScrollItem;
		private int tvScrollPosition;

		private const int autoScrollMode = -1;
		private const int manualScrollModeUp = -2;
		private const int manualScrollModeDown = -3;

		// Сформированные контекстные меню
		private List<List<string>> tapMenuItems = [];
		private List<string> pageVariants = [];
		private List<string> pictureBKVariants = [];
		private List<string> pictureBKSelectionVariants = [];
		private List<string> pictureTAVariants = [];
		private List<string> pictureTASelectionVariants = [];
		private List<string> censorshipVariants = [];
		private List<string> logColorVariants = [];
		private List<string> logFontFamilyVariants = [];

		// Последняя использованная категория
		private string lastCategory = "";
		private int lastCategoryIndex = -1;
		private int lastTopCategoryIndex = -1;

		private const uint categoriesPerPage = 20;
		private uint currentCategoriesPage = 0;
		private uint categoriesPagesCount = 0;

		// Список полученных категорий
		private string[] categoriesReqResult;

		// Цветовая схема
		private readonly Color logMasterBackColor = Color.FromArgb ("#F0F0F0");
		private readonly Color logFieldBackColor = Color.FromArgb ("#80808080");

		private readonly Color settingsMasterBackColor = Color.FromArgb ("#FFF8F0");
		private readonly Color settingsFieldBackColor = Color.FromArgb ("#FFE8D0");

		private readonly Color aboutMasterBackColor = Color.FromArgb ("#F0FFF0");
		private readonly Color aboutFieldBackColor = Color.FromArgb ("#D0FFD0");

		private readonly Color categoryMasterBackColor = Color.FromArgb ("#F8FFF0");
		private readonly Color categoryFieldBackColor = Color.FromArgb ("#E8FFD0");

		#endregion

		#region Переменные страниц

		private ContentPage settingsPage, aboutPage, logPage, categoryPage;

		private Label fontSizeFieldLabel, groupSizeFieldLabel, aboutFontSizeField,
			genCategoryEmpty, genCategoryLabel, genCatCurrentPage;

		private Switch newsAtTheEndSwitch, keepScreenOnSwitch, enableCopySubscriptionSwitch,
			translucencySwitch, offlineModeSwitch, shortLogSwitch;

		private Button centerButton, scrollUpButton, scrollDownButton, menuButton, sameCatButton,
			pictureBackButton, pTextOnTheLeftButton, censorshipButton, logColorButton,
			pSubsButton, logFontFamilyButton, lastUsedCategory, genCatPrevPage, genCatNextPage;

		private List<Button> topCategories = [];
		private List<Button> genCategories = [];

		private FlexLayout topCategorySection, genCategorySection;

		private ListView mainLog;

		#endregion

		#region Запуск и настройка

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
			{
			// Инициализация
			InitializeComponent ();
			}

		// Замена определению MainPage = new MasterPage ()
		protected override Window CreateWindow (IActivationState activationState)
			{
			return new Window (AppShell ());
			}

		// Настройка страниц приложения
		private Page AppShell ()
			{
			Page mainPage = new MasterPage ();
			flags = RDGenerics.GetAppStartupFlags (RDAppStartupFlags.DisableXPUN | RDAppStartupFlags.CanWriteFiles);

			if (!RDLocale.IsCurrentLanguageRuRu)
				RDLocale.CurrentLanguage = RDLanguages.ru_ru;

			// Общая конструкция страниц приложения
			settingsPage = RDInterface.ApplyPageSettings (new SettingsPage (),
				"Настройки приложения", settingsMasterBackColor);
			aboutPage = RDInterface.ApplyPageSettings (new AboutPage (),
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout), aboutMasterBackColor);
			logPage = RDInterface.ApplyPageSettings (new LogPage (),
				"Журнал", logMasterBackColor);
			categoryPage = RDInterface.ApplyPageSettings (new CategoryPage (),
				"Категории", categoryMasterBackColor);

			RDInterface.SetMasterPage (mainPage, logPage, logMasterBackColor);

			if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.StartupTips))
				RDInterface.SetCurrentPage (settingsPage, settingsMasterBackColor);

			#region Страница настроек

			RDInterface.ApplyLabelSettings (settingsPage, "AppSettingsLabel",
				"Просмотр", RDLabelTypes.HeaderLeft);

			// Запрет спящего режима
			RDInterface.ApplyLabelSettings (settingsPage, "KeepScreenOnLabel",
				"Запрет спящего режима", RDLabelTypes.DefaultLeft);
			keepScreenOnSwitch = RDInterface.ApplySwitchSettings (settingsPage, "KeepScreenOnSwitch",
				false, settingsFieldBackColor, KeepScreenOnSwitch_Toggled, RDInterface.KeepScreenOn);
			RDInterface.ApplyLabelSettings (settingsPage, "KeepScreenOnTip",
				"Опция запрещает переход устройства в спящий режим, пока приложение открыто, " +
				"позволяя экрану оставаться активным, пока Вы читаете тексты записей",
				RDLabelTypes.TipJustify);

			RDInterface.ApplyLabelSettings (settingsPage, "GenericSettingsLabel",
				"Интерфейс", RDLabelTypes.HeaderLeft);

			RDInterface.ApplyLabelSettings (settingsPage, "RestartTipLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Message_RestartRequired),
				RDLabelTypes.TipCenter);

			RDInterface.ApplyLabelSettings (settingsPage, "FontSizeLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceFontSize),
				RDLabelTypes.DefaultLeft);
			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeInc",
				RDDefaultButtons.Increase, settingsFieldBackColor, FontSizeButton_Clicked);
			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeDec",
				RDDefaultButtons.Decrease, settingsFieldBackColor, FontSizeButton_Clicked);
			aboutFontSizeField = RDInterface.ApplyLabelSettings (settingsPage, "FontSizeField",
				" ", RDLabelTypes.DefaultCenter);
			RDInterface.ApplyLabelSettings (settingsPage, "FontSizeTipLabel",
				"Эта настройка задаёт размер шрифта во всех разделах приложения, кроме журнала. " +
				"Измените её, если она не соответствует Вашему устройству",
				RDLabelTypes.TipJustify);

			// Оффлайн-режим
			RDInterface.ApplyLabelSettings (settingsPage, "OfflineModeLabel",
				"Оффлайн-режим", RDLabelTypes.DefaultLeft);
			offlineModeSwitch = RDInterface.ApplySwitchSettings (settingsPage,
				"OfflineModeSwitch", false, settingsFieldBackColor,
				OfflineMode_Toggled, GMJ.EnableOfflineMode);
			RDInterface.ApplyLabelSettings (settingsPage, "OfflineModeTip",
				GMJ.OfflineModeTip, RDLabelTypes.TipJustify);

			// Ссылка на оригинал
			Label eps1 = RDInterface.ApplyLabelSettings (settingsPage, "EnablePostSubscriptionLabel",
				"Ссылка на оригинал", RDLabelTypes.DefaultLeft);
			enableCopySubscriptionSwitch = RDInterface.ApplySwitchSettings (settingsPage,
				"EnablePostSubscriptionSwitch", false, settingsFieldBackColor,
				EnablePostSubscription_Toggled, GMJ.EnableCopySubscription);
			Label eps2 = RDInterface.ApplyLabelSettings (settingsPage, "EnablePostSubscriptionTip",
				GMJ.EnablePostSubscriptionTip, RDLabelTypes.TipJustify);

			if (RDGenerics.IsTV)
				{
				GMJ.EnableCopySubscription = false;
				eps1.IsVisible = eps2.IsVisible = enableCopySubscriptionSwitch.IsVisible = false;
				}

			#endregion

			#region Страница "О программе"

			RDInterface.ApplyLabelSettings (aboutPage, "AboutLabel",
				RDGenerics.AppAboutLabelText, RDLabelTypes.AppAbout);

			RDInterface.ApplyButtonSettings (aboutPage, "ManualsButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_ReferenceMaterials),
				aboutFieldBackColor, ReferenceButton_Click, false);

			Button hlp = RDInterface.ApplyButtonSettings (aboutPage, "HelpButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click, false);
			hlp.IsVisible = !RDGenerics.IsTV;

			Image qrImage = (Image)aboutPage.FindByName ("QRImage");
			qrImage.IsVisible = RDGenerics.IsTV;

			RDInterface.ApplyButtonSettings (aboutPage, "StatsButton",
				GMJ.GMJStatsMenuItem,
				aboutFieldBackColor, StatsButton_Click, false);

			RDInterface.ApplyLabelSettings (aboutPage, "HelpHeaderLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
				RDLabelTypes.HeaderLeft);
			Label htl = RDInterface.ApplyLabelSettings (aboutPage, "HelpTextLabel",
				RDGenerics.GetAppHelpText (), RDLabelTypes.SmallLeft);
			htl.TextType = TextType.Html;
			//htl.HorizontalTextAlignment = TextAlignment.Justify;	// Пока не работает

			FontSizeButton_Clicked (null, null);

			#endregion

			#region Страницы журнала и настроек приложения

			mainLog = (ListView)logPage.FindByName ("MainLog");
			mainLog.BackgroundColor = logFieldBackColor;
			mainLog.HasUnevenRows = true;
			mainLog.ItemTapped += MainLog_ItemTapped;
			mainLog.ItemTemplate = new DataTemplate (typeof (NotificationView));
			mainLog.SelectionMode = ListViewSelectionMode.None;
			mainLog.SeparatorVisibility = SeparatorVisibility.None;
			mainLog.ItemAppearing += MainLog_ItemAppearing;

			RDInterface.MasterPage.Popped += CurrentPageChanged;

			centerButton = RDInterface.ApplyButtonSettings (logPage, "CenterButton", " ",
				logFieldBackColor, CenterButton_Click, false);
			centerButton.FontSize += 6;

			scrollUpButton = RDInterface.ApplyButtonSettings (logPage, "ScrollUp",
				RDDefaultButtons.Up, logFieldBackColor, ScrollUpButton_Click);
			scrollDownButton = RDInterface.ApplyButtonSettings (logPage, "ScrollDown",
				RDDefaultButtons.Down, logFieldBackColor, ScrollDownButton_Click);
			centerButton.HeightRequest = centerButton.MaximumHeightRequest = scrollDownButton.HeightRequest;

			// Главный журнал
			RDInterface.ApplyLabelSettings (settingsPage, "LogSettingsLabel",
				"Журнал", RDLabelTypes.HeaderLeft);

			// Расположение новых записей в конце журнала
			Label nates1 = RDInterface.ApplyLabelSettings (settingsPage, "NewsAtTheEndLabel",
				"Новые записи – снизу", RDLabelTypes.DefaultLeft);
			newsAtTheEndSwitch = RDInterface.ApplySwitchSettings (settingsPage, "NewsAtTheEndSwitch",
				false, settingsFieldBackColor, NewsAtTheEndSwitch_Toggled, NotificationsSupport.LogNewsItemsAtTheEnd);
			Label nates2 = RDInterface.ApplyLabelSettings (settingsPage, "NewsAtTheEndTip",
				"Опция позволяет добавлять новые записи в конец журнала (снизу). Если выключена, " +
				"записи добавляются в начало журнала (сверху)", RDLabelTypes.TipJustify);

			if (RDGenerics.IsTV)
				{
				nates1.IsVisible = nates2.IsVisible = newsAtTheEndSwitch.IsVisible = false;
				if (!NotificationsSupport.LogNewsItemsAtTheEnd)
					NotificationsSupport.LogNewsItemsAtTheEnd = true;
				}

			// Короткий журнал
			RDInterface.ApplyLabelSettings (settingsPage, "ShortLogLabel",
				"Режим одной записи", RDLabelTypes.DefaultLeft);
			shortLogSwitch = RDInterface.ApplySwitchSettings (settingsPage, "ShortLogSwitch",
				false, settingsFieldBackColor, ShortLogSwitch_Toggled, false /*NotificationsSupport.ShortLog*/);
			shortLogSwitch.IsEnabled = false;
			RDInterface.ApplyLabelSettings (settingsPage, "ShortLogTip",
				"Опция позволяет заменить журнал записей экраном, на котором отображается только одна запись за раз. " +
				"К сожалению, текущая версия MAUI непригодна для реализации этой опции",
				RDLabelTypes.TipJustify);

			// Цвет фона журнала
			RDInterface.ApplyLabelSettings (settingsPage, "LogColorLabel",
				"Цветовая тема:", RDLabelTypes.DefaultLeft);
			logColorButton = RDInterface.ApplyButtonSettings (settingsPage, "LogColorButton",
				" ", settingsFieldBackColor, LogColor_Clicked, false);
			RDInterface.ApplyLabelSettings (settingsPage, "LogColorTip",
				NotificationsSupport.LogColorTip, RDLabelTypes.TipJustify);

			// Кнопки меню и предложения в журнале
			menuButton = RDInterface.ApplyButtonSettings (logPage, "MenuButton",
				RDDefaultButtons.Menu, logFieldBackColor, SelectPage);
			sameCatButton = RDInterface.ApplyButtonSettings (logPage, "SameCatButton",
				RDDefaultButtons.Refresh, logFieldBackColor, LastUsedCategory_Clicked);
			scrollUpButton.IsVisible = scrollDownButton.IsVisible = RDGenerics.IsTV;

			// Режим полупрозрачности
			RDInterface.ApplyLabelSettings (settingsPage, "TranslucencyLabel",
				"Полупрозрачность журнала", RDLabelTypes.DefaultLeft);
			translucencySwitch = RDInterface.ApplySwitchSettings (settingsPage,
				"TranslucencySwitch", false, settingsFieldBackColor,
				Translucency_Toggled, NotificationsSupport.TranslucentLogItems);
			RDInterface.ApplyLabelSettings (settingsPage, "TranslucencyTip",
				NotificationsSupport.TranslucencyTip, RDLabelTypes.TipJustify);

			LogColor_Clicked (null, null);

			// Размер шрифта журнала
			fontSizeFieldLabel = RDInterface.ApplyLabelSettings (settingsPage, "FontSizeFieldLabel",
				"", RDLabelTypes.DefaultLeft);
			fontSizeFieldLabel.TextType = TextType.Html;

			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeIncButton",
				RDDefaultButtons.Increase, settingsFieldBackColor, FontSizeChanged);
			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeDecButton",
				RDDefaultButtons.Decrease, settingsFieldBackColor, FontSizeChanged);

			RDInterface.ApplyLabelSettings (settingsPage, "FontSizeFieldTip",
				NotificationsSupport.FontSizeFieldTip, RDLabelTypes.TipJustify);

			FontSizeChanged (null, null);

			// Размер группы запрашиваемых записей
			groupSizeFieldLabel = RDInterface.ApplyLabelSettings (settingsPage, "GroupSizeFieldLabel",
				"", RDLabelTypes.DefaultLeft);
			groupSizeFieldLabel.TextType = TextType.Html;

			RDInterface.ApplyButtonSettings (settingsPage, "GroupSizeIncButton",
				RDDefaultButtons.Increase, settingsFieldBackColor, GroupSizeChanged);
			RDInterface.ApplyButtonSettings (settingsPage, "GroupSizeDecButton",
				RDDefaultButtons.Decrease, settingsFieldBackColor, GroupSizeChanged);
			RDInterface.ApplyLabelSettings (settingsPage, "GroupSizeFieldTip",
				NotificationsSupport.GroupSizeFieldTip, RDLabelTypes.TipJustify);

			GroupSizeChanged (null, null);

			// Цензурирование
			censorshipButton = RDInterface.ApplyButtonSettings (settingsPage, "CensorshipButton",
				" ", settingsFieldBackColor, Censorship_Clicked, false);

			if (flags.HasFlag (RDAppStartupFlags.DisableXPUN))
				{
				RDInterface.ApplyLabelSettings (settingsPage, "CensorshipLabel",
					"Цензурирование: включено", RDLabelTypes.DefaultLeft);

				censorshipButton.IsEnabled = censorshipButton.IsVisible = false;
				if (!GMJ.EnableCensorship)
					GMJ.EnableCensorship = true;
				}
			else
				{
				RDInterface.ApplyLabelSettings (settingsPage, "CensorshipLabel",
					"Цензурирование:", RDLabelTypes.DefaultLeft);
				}
			RDInterface.ApplyLabelSettings (settingsPage, "CensorshipTip",
				GMJ.CensorshipTip + (flags.HasFlag (RDAppStartupFlags.DisableXPUN) ?
				". В данной версии приложения опция заблокирована в положении «включена» " +
				"в связи с возрастным рейтингом, предоставленным магазином приложений" : ""),
				RDLabelTypes.TipJustify);

			Censorship_Clicked (null, null);

			// Шрифт журнала
			RDInterface.ApplyLabelSettings (settingsPage, "LogFontFamilyLabel",
				"Шрифт:", RDLabelTypes.DefaultLeft);
			logFontFamilyButton = RDInterface.ApplyButtonSettings (settingsPage, "LogFontFamilyButton",
				" ", settingsFieldBackColor, LogFontFamily_Clicked, false);
			RDInterface.ApplyLabelSettings (settingsPage, "LogFontFamilyTip",
				"Опция задаёт шрифт текста в журнале: " +
				"Roboto – без засечек (несколько яркостей), " +
				"Condensed – без засечек узкий (несколько яркостей), " +
				"Noto – с засечками, " +
				"Droid Sans – без засечек моноширинный",
				RDLabelTypes.TipJustify);

			LogFontFamily_Clicked (null, null);

			// Настройки картинок
			Label pictLabel = RDInterface.ApplyLabelSettings (settingsPage, "PicturesLabel",
				"Сохраняемые картинки", RDLabelTypes.HeaderLeft);

			// Фон картинок
			Label pictBackLabel1 = RDInterface.ApplyLabelSettings (settingsPage, "PicturesBackLabel",
				"Фон:", RDLabelTypes.DefaultLeft);
			pictureBackButton = RDInterface.ApplyButtonSettings (settingsPage, "PicturesBackButton",
				" ", settingsFieldBackColor, PictureBack_Clicked, false);
			Label pictBackLabel2 = RDInterface.ApplyLabelSettings (settingsPage, "PicturesBackTip",
				NotificationsSupport.PicturesBackTip, RDLabelTypes.TipJustify);

			// Выравнивание текста
			Label pictTextLabel1 = RDInterface.ApplyLabelSettings (settingsPage, "PTextLeftLabel",
				"Выравнивание:", RDLabelTypes.DefaultLeft);
			pTextOnTheLeftButton = RDInterface.ApplyButtonSettings (settingsPage, "PTextLeftButton",
				" ", settingsFieldBackColor, PTextOnTheLeft_Toggled, false);
			Label pictTextLabel2 = RDInterface.ApplyLabelSettings (settingsPage, "PTextLeftTip",
				NotificationsSupport.PicturesTextAlignmentTip, RDLabelTypes.TipJustify);

			// Подпись картинок
			Label pictSubsLabel1 = RDInterface.ApplyLabelSettings (settingsPage, "PSubsLabel",
				"Подпись:", RDLabelTypes.DefaultLeft);
			pSubsButton = RDInterface.ApplyButtonSettings (settingsPage, "PSubsButton",
				" ", settingsFieldBackColor, PSubs_Clicked, false);
			Label pictSubsLabel2 = RDInterface.ApplyLabelSettings (settingsPage, "PSubsTip",
				NotificationsSupport.PicturesSubscriptionTip, RDLabelTypes.TipJustify);

			if (RDGenerics.IsTV)
				{
				pictLabel.IsVisible =
					pictBackLabel1.IsVisible = pictBackLabel2.IsVisible = pictureBackButton.IsVisible =
					pictTextLabel1.IsVisible = pictTextLabel2.IsVisible = pTextOnTheLeftButton.IsVisible =
					pictSubsLabel1.IsVisible = pictSubsLabel2.IsVisible = pSubsButton.IsVisible = false;
				}
			else
				{
				PictureBack_Clicked (null, null);
				PTextOnTheLeft_Toggled (null, null);
				PSubs_Clicked (null, null);
				}

			#endregion

			#region Страница категорий

			// Категории верхнего уровня
			RDInterface.ApplyLabelSettings (categoryPage, "TopCategoryLabel",
				"Группы категорий:", RDLabelTypes.HeaderLeft);
			topCategorySection = (FlexLayout)categoryPage.FindByName ("TopCategorySection");

			string[] topCat = GMJ.GetTopCategories ();  // Активация списка
			for (int i = 0; i < topCat.Length; i++)
				{
				Button b = new Button ();
				b.BackgroundColor = categoryFieldBackColor;
				b.FontAttributes = FontAttributes.None;
				b.FontSize = 5 * RDInterface.MasterFontSize / 4;
				b.TextColor = RDInterface.GetInterfaceColor (RDInterfaceColors.AndroidTextColor);
				b.WidthRequest = b.HeightRequest = RDInterface.MasterFontSize * 2.75;
				b.Padding = Thickness.Zero;
				b.Margin = new Thickness (3);
				b.Text = topCat[i];
				b.TextTransform = TextTransform.None;
				b.Clicked += SelectTopCategory;

				topCategories.Add (b);
				topCategorySection.Add (b);
				}

			genCategoryLabel = RDInterface.ApplyLabelSettings (categoryPage, "GenCategoryLabel",
				"Категории:", RDLabelTypes.HeaderLeft);
			genCategoryLabel.IsVisible = false;

			genCatPrevPage = RDInterface.ApplyButtonSettings (categoryPage, "GenCatPrevPage",
				RDDefaultButtons.Backward, categoryFieldBackColor, ChangeCatPage_Clicked);
			genCatCurrentPage = RDInterface.ApplyLabelSettings (categoryPage, "GenCatCurrentPage",
				" ", RDLabelTypes.HeaderCenter);
			genCatNextPage = RDInterface.ApplyButtonSettings (categoryPage, "GenCatNextPage",
				RDDefaultButtons.Start, categoryFieldBackColor, ChangeCatPage_Clicked);
			genCatPrevPage.IsVisible = genCatNextPage.IsVisible = genCatCurrentPage.IsVisible = false;

			genCategorySection = (FlexLayout)categoryPage.FindByName ("GenCategorySection");
			genCategorySection.IsVisible = false;

			genCategoryEmpty = RDInterface.ApplyLabelSettings (categoryPage, "GenCategoryEmpty",
				"(все записи из этой категории уже просмотрены)", RDLabelTypes.TipCenter);
			genCategoryEmpty.IsVisible = false;

			lastUsedCategory = RDInterface.ApplyButtonSettings (categoryPage, "LastUsedCategory",
				"Последняя выбранная категория", categoryFieldBackColor, LastUsedCategory_Clicked, false);
			lastUsedCategory.IsVisible = false;

			#endregion

			// Запуск цикла обратной связи (без ожидания)
			FinishBackgroundRequest ();

			// Принятие соглашений
			ShowStartupTips ();
			return mainPage;
			}

		// Исправление для сброса текущей позиции журнала
		private async void CurrentPageChanged (object sender, EventArgs e)
			{
			// Автопрокрутка при возврате на главную страницу (только если не выполняются фоновые процессы)
			if ((RDInterface.MasterPage.CurrentPage == logPage) && centerButtonEnabled)
				{
				needsScroll = true;
				await ScrollMainLog (autoScrollMode);
				}
			}

		// Цикл обратной связи для загрузки текущего журнала, если фоновая служба не успела завершить работу
		private bool FinishBackgroundRequest ()
			{
			// Ожидание завершения операции
			SetLogState (false);

			UpdateLogButton (true, true);

			// Перезапрос журнала
			if (masterLog != null)
				masterLog.Clear ();
			masterLog = new List<MainLogItem> (NotificationsSupport.GetMasterLog (true));

			needsScroll = true;
			UpdateLog ();

			// Настройка и запуск
			SetLogState (true);
			return true;
			}

		// Метод отображает подсказки при первом запуске
		private async void ShowStartupTips ()
			{
			// Контроль XPUN
			if (!flags.HasFlag (RDAppStartupFlags.DisableXPUN))
				await RDInterface.XPUNLoop ();

			// Требование принятия Политики
			if (!RDGenerics.IsTV)
				await RDInterface.PolicyLoop ();    // Выставляет бит 0 в TipsState автоматически

			// Подсказки
			if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.StartupTips))
				{
				await RDInterface.ShowMessage ("Добро пожаловать в мини-клиент канала JokesArray!" + RDLocale.RNRN +
					"• На этой странице Вы можете настроить поведение приложения." + RDLocale.RNRN +
					"• Используйте системную кнопку «Назад», чтобы вернуться к журналу записей " +
					"из любого раздела." + RDLocale.RNRN +
					"• Используйте кнопку с семафором для получения случайных записей из архива",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next));

				if (RDGenerics.IsTV)
					{
					await RDInterface.ShowMessage ("Внимание!" + RDLocale.RNRN +
						"• Убедитесь, что данное устройство имеет выход в интернет. Если такой возможности нет, " +
						"не забудьте включить оффлайн-режим в настройках приложения." + RDLocale.RNRN +
						"• Ознакомьтесь с описанием проекта в разделе «О приложении» (кнопка ≡). Убедитесь, " +
						"что Вы согласны с Политикой Лаборатории",
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
					}
				else
					{
					await RDInterface.ShowMessage ("Внимание!" + RDLocale.RNRN +
						"Некоторые устройства требуют ручного разрешения на доступ в интернет " +
						"(например, если активен режим экономии интернет-трафика). Проверьте его, " +
						"если онлайн-запросы не будут работать правильно",
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
					}

				RDGenerics.TipsState |= (uint)NSTipTypes.StartupTips;
				}
			}

		// Метод отображает остальные подсказки
		private static async Task<bool> ShowTips (NSTipTypes Type)
			{
			// Подсказки
			string msg = "";
			switch (Type)
				{
				case NSTipTypes.GoToButton:
					msg = "Эта опция позволяет перейти к оригиналу выбранной записи " +
						"на нашем Telegram-канале";
					break;

				case NSTipTypes.ShareTextButton:
					msg = "Эта опция позволяет поделиться текстом записи";
					if (GMJ.EnableCopySubscription)
						msg += ("." + RDLocale.RNRN +
							"Обратите внимание, что приложение добавляет к текстам, которыми Вы делитесь, " +
							"ссылку на канал JokesArray");
					break;

				case NSTipTypes.ShareImageButton:
					msg = "Эта опция позволяет поделиться записью в виде картинки";
					break;

				case NSTipTypes.MainLogClickMenuTip:
					msg = "Все операции с текстами записей доступны по клику на них в журнале приложения";
					break;

				case NSTipTypes.PostSubscriptions:
					msg = GMJ.PostSubscriptionDisclaimer;
					break;
				}

			await RDInterface.ShowMessage (msg, RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
			RDGenerics.TipsState |= (uint)Type;
			return true;
			}

		/// <summary>
		/// Сохранение настроек программы
		/// </summary>
		protected override void OnSleep ()
			{
			RDGenerics.StopRequested = true;
			NotificationsSupport.SetMasterLog (masterLog);
			}

		/// <summary>
		/// Возврат в интерфейс при сворачивании
		/// </summary>
		protected override void OnResume ()
			{
			RDInterface.MasterPage.PopToRootAsync (true);

			// Запуск цикла обратной связи (без ожидания, на случай, если приложение было свёрнуто, но не закрыто,
			// а во время ожидания имели место обновления журнала)
			FinishBackgroundRequest ();
			}

		/// <summary>
		/// Возврат в интерфейс из статичного оповещения (использует перенаправление в MasterPage)
		/// </summary>
		public void ResumeApp ()
			{
			OnResume ();
			}

		#endregion

		#region Журнал

		// Принудительное обновление лога
		private void UpdateLog ()
			{
			UpdateLog (-1);
			}

		private void UpdateLog (int ScrollPosition)
			{
			// Обновление журнала
			tvScrollPosition = ScrollPosition;
			mainLog.ItemsSource = null;
			mainLog.ItemsSource = masterLog;
			}

		// Промотка журнала к нужной позиции
		private async void MainLog_ItemAppearing (object sender, ItemVisibilityEventArgs e)
			{
			if (tvScrollPosition >= 0)
				await ScrollMainLog (tvScrollPosition);
			else
				await ScrollMainLog (NotificationsSupport.LogNewsItemsAtTheEnd ? masterLog.Count - 1 : 0);
			}

		private async Task<bool> ScrollMainLog (int VisibleItem)
			{
			// Контроль
			if (masterLog == null)
				return false;

			if ((masterLog.Count < 1) || !needsScroll)
				return false;

			// Искусственная задержка
			await Task.Delay (100);

			// Промотка с повторением до достижения нужного участка
			if (VisibleItem <= autoScrollMode)
				needsScroll = false;

			// Определение варианта промотки
			bool toTheEnd = NotificationsSupport.LogNewsItemsAtTheEnd;
			if (VisibleItem > manualScrollModeUp)
				{
				currentScrollItem = VisibleItem;
				if (currentScrollItem < 0)
					currentScrollItem = toTheEnd ? (masterLog.Count - 1) : 0;
				if (currentScrollItem > masterLog.Count - 1)
					currentScrollItem = masterLog.Count - 1;
				}
			else if (VisibleItem > manualScrollModeDown)
				{
				if (currentScrollItem > 0)
					currentScrollItem--;
				}
			else
				{
				if (currentScrollItem < (masterLog.Count - 1))
					currentScrollItem++;
				}

			if (toTheEnd)
				{
				if (VisibleItem > masterLog.Count - 3)
					needsScroll = false;
				}
			else
				{
				if (VisibleItem < 2)
					needsScroll = false;
				}

			try
				{
				mainLog.ScrollTo (masterLog[currentScrollItem], ScrollToPosition.MakeVisible,
					VisibleItem <= manualScrollModeUp);
				}
			catch { }
			return true;
			}

		// Обновление формы кнопки журнала
		private void UpdateLogButton (bool Requesting, bool FinishingBackgroundRequest)
			{
			bool red = Requesting && FinishingBackgroundRequest;
			bool yellow = Requesting && !FinishingBackgroundRequest;
			bool green = !Requesting && !FinishingBackgroundRequest;
			bool dark = !NotificationsSupport.LogColors.CurrentColor.IsBright;

			if (red || yellow || green)
				{
				centerButton.Text = (red ? semaphoreOn : semaphoreOff) + (yellow ? semaphoreOn : semaphoreOff) +
					(green ? semaphoreOn : semaphoreOff);

				if (red)
					centerButton.TextColor = Color.FromArgb (dark ? "#FF4040" : "#D00000");
				else if (yellow)
					centerButton.TextColor = Color.FromArgb (dark ? "#FFFF40" : "#D0D000");
				else
					centerButton.TextColor = Color.FromArgb (dark ? "#40FF40" : "#00D000");
				}
			else
				{
				centerButton.Text = "   ";
				}
			}

		// Выбор оповещения для перехода или share
		private async void MainLog_ItemTapped (object sender, ItemTappedEventArgs e)
			{
			// Контроль
			if (RDGenerics.IsTV)
				return;

			MainLogItem notItem = (MainLogItem)e.Item;
			if (!centerButtonEnabled || (notItem.StringForSaving == ""))  // Признак разделителя
				return;

			// Сброс состояния
			UpdateLogButton (false, false);

			// Извлечение ссылки и номера оповещения
			string notLink = "";
			int l, r;
			if (((l = notItem.Header.IndexOf (GMJ.NumberStringBeginning)) >= 0) &&
				((r = notItem.Header.IndexOf (GMJ.NumberStringEnding, l)) >= 0))
				{
				l += GMJ.NumberStringBeginning.Length;
				notLink = GMJ.SourceRedirectLink + "/" + notItem.Header.Substring (l, r - l);
				}

			// Формирование меню
			const string secondMenuName = "🔣\t Ещё";
			const string copyTextName = "📑\t Скопировать текст";
			const string shareTextName = "📣\t Поделиться текстом";
			const string originalName = "➡️\t Перейти к оригиналу";
			if (tapMenuItems.Count < 1)
				{
				tapMenuItems.Add ([
					shareTextName,
					copyTextName,
					secondMenuName,
					]);
				tapMenuItems.Add ([
					originalName,
					shareTextName,
					copyTextName,
					secondMenuName,
					]);
				tapMenuItems.Add ([
					originalName,
					shareTextName,
					"🌅\t Поделиться картинкой",
					copyTextName,
					secondMenuName,
					]);
				tapMenuItems.Add ([
					"❌\t Удалить из журнала",
					"❌\t Очистить журнал",
					]);
				}

			// Запрос варианта использования
			int menuVariant = 0;
			if (!string.IsNullOrWhiteSpace (notLink))
				{
				menuVariant++;
				if (GMJPicture.AlignString (notItem.Text) != null)
					menuVariant++;
				}

			int menuItem = await RDInterface.ShowList ("Выберите действие:",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				tapMenuItems[menuVariant]);

			if (menuItem < 0)
				return;

			bool secondMenu = (tapMenuItems[menuVariant][menuItem] == secondMenuName);

			// Контроль второго набора
			if (secondMenu)
				{
				menuVariant = 3;

				menuItem = await RDInterface.ShowList ("Выберите действие:",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), tapMenuItems[menuVariant]);
				if (menuItem < 0)
					return;
				}

			// Окончательный выбор варианта действия
			menuVariant = menuItem + 10 * (menuVariant + 1);

			// Полный текст поста
			string notText = notItem.Header + RDLocale.RNRN + notItem.Text;
			if (!string.IsNullOrWhiteSpace (notItem.Separator))
				notText += (RDLocale.RNRN + notItem.Separator.Replace (RDLocale.RN, ""));
			if (GMJ.EnableCopySubscription)
				notText += (RDLocale.RNRN + notLink);
			notText = notText.Replace ("\r", "");

			// Обработка (неподходящие варианты будут отброшены)
			switch (menuVariant)
				{
				// Переход по ссылке
				case 20:
				case 30:
					if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.GoToButton))
						await ShowTips (NSTipTypes.GoToButton);

					if (GMJ.EnableCensorship)
						await RDInterface.ShowMessage (GMJ.CensorshipGoToChannelMessage,
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

					await RDGenerics.RunURL (notLink, true);
					break;

				// Поделиться
				case 10:
				case 21:
				case 31:
					if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.ShareTextButton))
						await ShowTips (NSTipTypes.ShareTextButton);

					await Share.RequestAsync (notText, ProgramDescription.AssemblyVisibleName);
					break;

				// Скопировать в буфер обмена
				case 11:
				case 22:
				case 33:
					RDGenerics.SendToClipboard (notText, true);
					break;

				// Поделиться картинкой
				case 32:
					if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.ShareImageButton))
						await ShowTips (NSTipTypes.ShareImageButton);

					if (!flags.HasFlag (RDAppStartupFlags.CanWriteFiles))
						{
						if (await RDInterface.ShowMessage (
							RDLocale.GetDefaultText (RDLDefaultTexts.Message_ReadWritePermission) + "." +
							RDLocale.RNRN + "Перейти к настройкам разрешений приложения?",
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
							RDInterface.CallAppSettings ();
						return;
						}

					GMJPictureTextAlignment pta = NotificationsSupport.PicturesTextAlignment;
					if (pta == GMJPictureTextAlignment.AskUser)
						{
						int res = await RDInterface.ShowList ("Выровнять текст:",
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pictureTASelectionVariants);
						if (res < 0)
							return;

						pta = (GMJPictureTextAlignment)res;
						}

					int pbk;
					switch (NotificationsSupport.PicturesBackgroundType)
						{
						case NotificationsSupport.PicturesBackgroundAsk:
							int res = await RDInterface.ShowList ("Использовать фон:",
								RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pictureBKSelectionVariants);
							if (res < 0)
								return;

							pbk = res;
							break;

						case NotificationsSupport.PicturesBackgroundRandom:
							pbk = RDGenerics.RND.Next (NotificationsSupport.PictureColors.ColorNames.Length);
							break;

						default:
							pbk = NotificationsSupport.PicturesBackgroundType;
							break;
						}

					var pict = GMJPicture.CreateRecordPicture (notItem.Header, notItem.Text,
						notItem.SeparatorIsSign ? notItem.Separator.Replace (RDLocale.RN, "") : "",
						pta, NotificationsSupport.PictureColors.GetColor ((uint)pbk));
					if (pict == null)
						{
						RDInterface.ShowBalloon ("Текст записи не позволяет сформировать картинку", true);
						return;
						}

					await GMJPicture.SaveRecordPictureToFile (pict, notItem.Header);
					break;

				// Удаление из журнала
				case 40:
					masterLog.RemoveAt (e.ItemIndex);
					UpdateLog ();
					break;

				// Очистка журнала
				case 41:
					if (await RDInterface.ShowMessage ("Очистить журнал?",
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
						{
						masterLog.Clear ();
						UpdateLog ();
						}
					break;
				}

			// Завершено
			}

		// Блокировка / разблокировка кнопок
		private void SetLogState (bool State)
			{
			// Переключение состояния кнопок и свичей
			centerButtonEnabled = State;
			menuButton.IsVisible = scrollDownButton.IsVisible = scrollUpButton.IsVisible =
				sameCatButton.IsVisible = State;
			scrollUpButton.IsVisible = scrollDownButton.IsVisible = State && RDGenerics.IsTV;

			// Обновление статуса
			UpdateLogButton (!State, false);
			}

		// Добавление текста в журнал
		private void AddTextToLog (string Text)
			{
			uint limit = /*NotificationsSupport.ShortLog ? 1 :*/ NotificationsSupport.MasterLogMaxItems;
			if (NotificationsSupport.LogNewsItemsAtTheEnd)
				{
				masterLog.Add (new MainLogItem (Text));

				// Удаление верхних строк
				while (masterLog.Count > limit)
					masterLog.RemoveAt (0);
				}
			else
				{
				masterLog.Insert (0, new MainLogItem (Text));

				// Удаление нижних строк (здесь требуется, т.к. не выполняется обрезка свойством .MainLog)
				while (masterLog.Count > limit)
					masterLog.RemoveAt (masterLog.Count - 1);
				}
			}

		// Действия средней кнопки журнала
		private void CenterButton_Click (object sender, EventArgs e)
			{
			if (!centerButtonEnabled)
				{
				RDInterface.ShowBalloon ("Пожалуйста, дождитесь ответа на запрос...", true);
				return;
				}

			GetRecord (false);
			}

		private async void GetRecord (bool FromCategory)
			{
			// Блокировка на время опроса
			SetLogState (false);
			if (FromCategory)
				RDInterface.ShowBalloon ("Запрос записи...", false);
			else
				RDInterface.ShowBalloon ("Запрос случайной записи...", false);

			// Запуск и разбор
			RDGenerics.StopRequested = false; // Разблокировка метода GetHTML
			string newText = "";
			uint group = NotificationsSupport.GroupSize;
			bool success = false;

			for (int i = 0; i < group; i++)
				{
				// Антиспам
				if (i > 0)
					Thread.Sleep (1000);

				newText = await Task.Run<string> (GMJ.GetRandomRecord);
				if (newText == "")
					{
					RDInterface.ShowBalloon (GMJ.NoConnectionPattern, false);
					}
				else if (newText.Contains (GMJ.SourceNoReturnPattern))
					{
					RDInterface.ShowBalloon (newText, false);
					}
				else
					{
					// Разбиение на экраны
					if (RDGenerics.IsTV)
						{
						int left;
						const int linesLimit = 9;

						// NotificationsSupport.LogNewsItemsAtTheEnd is true
						int scrollTo = masterLog.Count;

						int charsLimit = 60;
						if (NotificationsSupport.LogFontSize > 20)
							charsLimit -= 2 * (int)(NotificationsSupport.LogFontSize - 20);
						if (charsLimit < 30)
							charsLimit = 30;

						do
							{
							// Поиск ближайшего подходящего абзаца
							left = -RDLocale.RN.Length;
							for (int l = 0; l < linesLimit; l++)
								{
								left = newText.IndexOf (RDLocale.RN, left + RDLocale.RN.Length);
								if ((left < 0) || (left > charsLimit * linesLimit))
									break;
								}

							// Отделение
							if (left < 0)
								{
								AddTextToLog (newText);
								}
							else
								{
								/*NotificationsSupport.ShortLog = false;	// Отмена для неодинарных записей*/
								AddTextToLog (newText.Substring (0, left));
								newText = NotificationsSupport.HeaderBeginning + "(продолжение)" +
									NotificationsSupport.HeaderEnding + MainLogItem.MainLogItemSplitter +
									newText.Substring (left + RDLocale.RN.Length);
								}

							// При достижении конца текста немедленное завершение
							if (left < 0)
								break;
							// (left > 0) обеспечивает обработку последнего фрагмента текста

							// NotificationsSupport.LogNewsItemsAtTheEnd is true
							scrollTo--;
							}
						while ((left > 0) || ((newText.Length - newText.Replace ("\n", "").Length > linesLimit) ||
							(newText.Length > charsLimit * linesLimit)));

						/*// Восстановление для неодинарных записей
						NotificationsSupport.ShortLog = shortLogSwitch.IsToggled;*/

						needsScroll = true;

						scrollTo--; // Пока не понятно, почему -1
						if (scrollTo < 0)
							scrollTo = 0;
						UpdateLog (scrollTo);
						}

					// Прямое направление
					else
						{
						AddTextToLog (newText);
						needsScroll = true;
						UpdateLog ();
						}

					// Завершено
					success = true;
					}
				}

			// Разблокировка
			SetLogState (true);
			UpdateLogButton (!success, !success);
			if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.MainLogClickMenuTip))
				await ShowTips (NSTipTypes.MainLogClickMenuTip);
			}

		// Ручная прокрутка
		private async void ScrollUpButton_Click (object sender, EventArgs e)
			{
			needsScroll = true;
			await ScrollMainLog (manualScrollModeUp);
			}

		private async void ScrollDownButton_Click (object sender, EventArgs e)
			{
			needsScroll = true;
			await ScrollMainLog (manualScrollModeDown);
			}

		// Выбор текущей страницы
		private async void SelectPage (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pageVariants.Count < 1)
				{
				pageVariants = [
					"🔄\t Та же категория",
					"🔍\t Все категории",
					"⚙️\t Настройки приложения",
					"ℹ️\t " + RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
					"🆕\t Предложить запись",
					];
				}

			int res = await RDInterface.ShowList (RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pageVariants);
			if (res < 0)
				return;

			// Вызов
			switch (res)
				{
				case 0:
					LastUsedCategory_Clicked (null, null);
					break;

				case 1:
					if (GMJ.RecordsLeft < 1)
						{
						await RDInterface.ShowMessage ("Архив записей ещё не обновлялся." + RDLocale.RNRN +
							"Нажмите кнопку с семафором, чтобы запросить первую запись из архива",
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
						return;
						}

					RDInterface.SetCurrentPage (categoryPage, categoryMasterBackColor);

					if (lastTopCategoryIndex >= 0)
						SelectTopCategory (topCategories[lastTopCategoryIndex], null);
					break;

				case 2:
					RDInterface.SetCurrentPage (settingsPage, settingsMasterBackColor);
					break;

				case 3:
					RDInterface.SetCurrentPage (aboutPage, aboutMasterBackColor);
					break;

				case 4:
					if (!await RDInterface.ShowMessage (GMJ.SuggestionMessage,
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
						return;

					await RDInterface.AskDeveloper ();
					break;
				}
			}

		#endregion

		#region Основные настройки

		// Включение / выключение фиксации экрана
		private void KeepScreenOnSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			RDInterface.KeepScreenOn = keepScreenOnSwitch.IsToggled;
			}

		// Включение / выключение добавления новостей с конца журнала
		private void NewsAtTheEndSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			// Обновление журнала
			if (e != null)
				NotificationsSupport.LogNewsItemsAtTheEnd = newsAtTheEndSwitch.IsToggled;

			UpdateLogButton (false, false);
			}

		// Включение / выключение режима короткого журнала
		private void ShortLogSwitch_Toggled (object sender, ToggledEventArgs e)
			{
			/*NotificationsSupport.ShortLog = shortLogSwitch.IsToggled;*/
			}

		// Изменение размера шрифта лога
		private void FontSizeChanged (object sender, EventArgs e)
			{
			uint fontSize = NotificationsSupport.LogFontSize;

			if (e != null)
				{
				Button b = (Button)sender;
				if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Increase) &&
					(fontSize < RDInterface.MaxFontSize))
					fontSize++;
				else if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Decrease) &&
					(fontSize > RDInterface.MinFontSize))
					fontSize--;

				NotificationsSupport.LogFontSize = fontSize;
				}

			// Принудительное обновление
			fontSizeFieldLabel.Text = string.Format ("Размер шрифта: <b>{0:D}</b>", fontSize.ToString ());

			if (e != null)
				{
				needsScroll = true;
				UpdateLog ();
				}
			}

		// Изменение размера шрифта лога
		private void GroupSizeChanged (object sender, EventArgs e)
			{
			uint groupSize = NotificationsSupport.GroupSize;

			if (e != null)
				{
				Button b = (Button)sender;
				if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Increase) && (groupSize < 5))
					groupSize++;
				else if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Decrease) && (groupSize > 1))
					groupSize--;

				NotificationsSupport.GroupSize = groupSize;
				}

			// Принудительное обновление
			groupSizeFieldLabel.Text = string.Format ("Длина серии: <b>{0:D}</b>", groupSize.ToString ());
			}

		// Включение / выключение подписи
		private async void EnablePostSubscription_Toggled (object sender, ToggledEventArgs e)
			{
			// Подсказки
			if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.PostSubscriptions))
				await ShowTips (NSTipTypes.PostSubscriptions);

			GMJ.EnableCopySubscription = enableCopySubscriptionSwitch.IsToggled;
			}

		// Включение / выключение оффлайн-режима
		private void OfflineMode_Toggled (object sender, ToggledEventArgs e)
			{
			GMJ.EnableOfflineMode = offlineModeSwitch.IsToggled;
			}

		// Выбор фона картинок
		private async void PictureBack_Clicked (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pictureBKVariants.Count < 1)
				{
				pictureBKVariants.Add ("(спрашивать)");
				pictureBKVariants.Add ("(случайный)");
				pictureBKVariants.AddRange (NotificationsSupport.PictureColors.ColorNames);
				pictureBKSelectionVariants.AddRange (NotificationsSupport.PictureColors.ColorNames);
				}

			int res;
			if (sender == null)
				{
				res = NotificationsSupport.PicturesBackgroundType - NotificationsSupport.PicturesBackgroundAsk;
				}
			else
				{
				res = await RDInterface.ShowList ("Фон картинок",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pictureBKVariants);
				if (res < 0)
					return;

				NotificationsSupport.PicturesBackgroundType = res + NotificationsSupport.PicturesBackgroundAsk;
				}

			pictureBackButton.Text = pictureBKVariants[res];
			}

		// Выбор режима выравнивания текста картинок
		private async void PTextOnTheLeft_Toggled (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pictureTAVariants.Count < 1)
				{
				pictureTAVariants = [
					"Всегда по центру",
					"Всегда по левой стороне",
					"Диалоги по левой стороне",
					"Запрашивать выравнивание",
					];
				pictureTASelectionVariants = [
					"По центру",
					"По левой стороне",
					];
				}

			int res;
			if (sender == null)
				{
				res = (int)NotificationsSupport.PicturesTextAlignment;
				}
			else
				{
				res = await RDInterface.ShowList ("Выравнивание текста",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pictureTAVariants);
				if (res < 0)
					return;

				NotificationsSupport.PicturesTextAlignment = (GMJPictureTextAlignment)res;
				if (NotificationsSupport.PicturesTextAlignment == GMJPictureTextAlignment.BasedOnDialogues)
					await RDInterface.ShowMessage (NotificationsSupport.PicturesTextAlignmentDialoguesTip,
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				}

			// Сохранение
			pTextOnTheLeftButton.Text = pictureTAVariants[res];
			}

		// Ввод подписи изображения
		private async void PSubs_Clicked (object sender, EventArgs e)
			{
			// Ввод подписи
			string sub;
			if (sender == null)
				{
				sub = NotificationsSupport.PicturesSubscription;
				}
			else
				{
				sub = await RDInterface.ShowInput ("Подпись картинок",
					"Введите подпись, которая будет добавляться на сохраняемые картинки",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Save),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
					NotificationsSupport.PicturesSubscriptionMaxLength,
					Keyboard.Text, NotificationsSupport.PicturesSubscription);

				// Если действие не было отменено
				if (sub == null)
					return;

				sub = sub.Replace ("\n", "").Replace ("\r", "").Replace ("\t", "");
				NotificationsSupport.PicturesSubscription = sub;
				}

			// Обработка и сохранение
			pSubsButton.Text = string.IsNullOrWhiteSpace (sub) ? "(нет)" : sub;
			}

		// Выбор режима цензурирования
		private async void Censorship_Clicked (object sender, EventArgs e)
			{
			// Запрос варианта
			if (censorshipVariants.Count < 1)
				{
				censorshipVariants = [
					"Отключено",
					"Действует",
					];
				}

			int res;
			if (sender == null)
				{
				res = GMJ.EnableCensorship ? 1 : 0;
				censorshipButton.Text = censorshipVariants[res];
				return;
				}

			res = await RDInterface.ShowList ("Цензурирование",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), censorshipVariants);
			if (res < 0)
				return;

			// Контроль
			string msg = (res > 0) ? GMJ.CensorshipEnableMessage : GMJ.CensorshipDisableMessage;
			bool doReset = false;
			if (await RDInterface.ShowMessage (msg, RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel)))
				{
				GMJ.EnableCensorship = (res > 0);
				censorshipButton.Text = censorshipVariants[res];
				doReset = true;
				}

			msg = (res > 0) ? GMJ.CensorshipEnableResetMessage : GMJ.CensorshipDisableResetMessage;
			if (doReset && await RDInterface.ShowMessage (msg, RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
				{
				GMJ.ResetFreeSet ();
				}
			}

		// Выбор фона картинок
		private async void LogColor_Clicked (object sender, EventArgs e)
			{
			// Запрос варианта
			if (logColorVariants.Count < 1)
				logColorVariants.AddRange (NotificationsSupport.LogColors.ColorNames);

			int res;
			if (sender == null)
				{
				res = (int)NotificationsSupport.LogColor;
				}
			else
				{
				res = await RDInterface.ShowList ("Цветовая тема журнала",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), logColorVariants);
				if (res < 0)
					return;

				NotificationsSupport.LogColor = (uint)res;
				}

			// Установка настроек
			GMJLogColor currentLogColor = NotificationsSupport.LogColors.CurrentColor;
			logColorButton.Text = logColorVariants[res];

			logPage.BackgroundColor = mainLog.BackgroundColor = centerButton.BackgroundColor =
				scrollUpButton.BackgroundColor = scrollDownButton.BackgroundColor =
				menuButton.BackgroundColor = sameCatButton.BackgroundColor = currentLogColor.BackColor;
			scrollUpButton.TextColor = scrollDownButton.TextColor = menuButton.TextColor =
				sameCatButton.TextColor = currentLogColor.MainTextColor;

			if (currentLogColor.IsBright)
				{
				RDInterface.MasterPage.BarBackgroundColor = currentLogColor.MainTextColor;
				RDInterface.MasterPage.BarTextColor = currentLogColor.BackColor;
				}
			else
				{
				RDInterface.MasterPage.BarBackgroundColor = currentLogColor.BackColor;
				RDInterface.MasterPage.BarTextColor = currentLogColor.MainTextColor;
				}

			// Принудительное обновление (только не при старте)
			if (sender != null)
				{
				needsScroll = true;
				UpdateLog ();
				}

			// Цепляет кнопку журнала
			UpdateLogButton (false, false);
			}

		private void Translucency_Toggled (object sender, EventArgs e)
			{
			NotificationsSupport.TranslucentLogItems = translucencySwitch.IsToggled;

			needsScroll = true;
			UpdateLog ();
			}

		// Изменение размера шрифта лога
		private async void LogFontFamily_Clicked (object sender, EventArgs e)
			{
			// Запрос варианта
			if (logFontFamilyVariants.Count < 1)
				{
				string[][] fonts = RDGenerics.AvailableFonts;
				for (int i = 0; i < fonts.Length; i++)
					{
					logFontFamilyVariants.Add (fonts[i][0]);
					logFontFamilyVariants.Add (fonts[i][0] + " Italic");
					}
				}

			int res;
			if (sender == null)
				{
				res = (int)RDGenerics.LogFontFamily;
				}
			else
				{
				res = await RDInterface.ShowList ("Шрифт журнала",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), logFontFamilyVariants);
				if (res < 0)
					return;

				RDGenerics.LogFontFamily = (uint)res;
				}

			// Сохранение и отображение настройки в интерфейсе
			logFontFamilyButton.Text = logFontFamilyVariants[res];

			string ff;
			FontAttributes fa;
			RDGenerics.GetCurrentFontFamily (out ff, out fa);

			logFontFamilyButton.FontAttributes = fa;
			logFontFamilyButton.FontFamily = ff;

			// Обновление журнала
			if (e != null)
				{
				needsScroll = true;
				UpdateLog ();
				}
			}

		#endregion

		#region О приложении

		// Вызов справочных материалов
		private async void ReferenceButton_Click (object sender, EventArgs e)
			{
			if (RDGenerics.IsTV)
				{
				await RDInterface.ShowMessage ("Для доступа к помощи, поддержке и справочным материалам " +
					"воспользуйтесь QR-кодом, представленным ниже." + RDLocale.RNRN +
					"Далее на этой странице доступно сокращённое описание приложения",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
				return;
				}

			await RDInterface.CallHelpMaterials (RDHelpMaterials.ReferenceMaterials);
			}

		private async void HelpButton_Click (object sender, EventArgs e)
			{
			await RDInterface.CallHelpMaterials (RDHelpMaterials.HelpAndSupport);
			}

		// Изменение размера шрифта интерфейса
		private void FontSizeButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					RDInterface.MasterFontSize += 0.5;
				else if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					RDInterface.MasterFontSize -= 0.5;
				}

			aboutFontSizeField.Text = RDInterface.MasterFontSize.ToString ("F1");
			aboutFontSizeField.FontSize = RDInterface.MasterFontSize;
			}

		// Отображение статистики архива
		private async void StatsButton_Click (object sender, EventArgs e)
			{
			await RDInterface.ShowMessage (GMJ.GMJStats, RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
			}

		#endregion

		#region Категории

		// Метод выполняет выбор категории верхнего уровня
		private async void SelectTopCategory (object sender, EventArgs e)
			{
			// Сброс списков
			genCategories.Clear ();
			genCategorySection.Clear ();

			// Запрос доступных категорий
			int idx = topCategories.IndexOf ((Button)sender);
			if (idx < 0)
				return;

			// Блокировка
			topCategorySection.IsEnabled = false;
			lastUsedCategory.IsVisible = false;
			genCatPrevPage.IsVisible = genCatNextPage.IsVisible = false;

			// Запрос
			lastTopCategoryIndex = idx;
			await Task.Run (CategoriesRequest);

			// Отображение кнопки последней категории, если возможно
			if ((lastCategoryIndex >= 0) && (lastCategoryIndex < categoriesReqResult.Length))
				lastUsedCategory.IsVisible = (categoriesReqResult[lastCategoryIndex] == lastCategory);

			// Отображение полей
			genCategoryEmpty.IsVisible = (categoriesReqResult.Length < 1);
			genCategoryLabel.IsVisible = genCatCurrentPage.IsVisible = genCategorySection.IsVisible =
				!genCategoryEmpty.IsVisible;
			if (genCategoryEmpty.IsVisible)
				{
				topCategorySection.IsEnabled = true;
				return;
				}

			// Загрузка
			genCategorySection.IsVisible = true;
			genCategoryEmpty.IsVisible = false;

			categoriesPagesCount = (uint)categoriesReqResult.Length / categoriesPerPage;
			if (categoriesReqResult.Length % categoriesPerPage != 0)
				categoriesPagesCount++;
			if (currentCategoriesPage >= categoriesPagesCount)
				currentCategoriesPage = 0;
			ChangeCatPage_Clicked (null, null);

			genCatPrevPage.IsVisible = genCatNextPage.IsVisible = (categoriesPagesCount > 1);

			for (int i = (int)(currentCategoriesPage * categoriesPerPage);
				(i < (currentCategoriesPage + 1) * categoriesPerPage) && (i < categoriesReqResult.Length); i++)
				{
				Button b = new Button ();
				b.BackgroundColor = categoryFieldBackColor;
				b.FontAttributes = FontAttributes.None;
				b.FontSize = RDInterface.MasterFontSize;
				b.HeightRequest = RDInterface.MasterFontSize * 2.75;
				b.TextColor = RDInterface.GetInterfaceColor (RDInterfaceColors.AndroidTextColor);
				b.Margin = b.Padding = new Thickness (3);
				b.Text = categoriesReqResult[i];
				b.TextTransform = TextTransform.None;
				b.Clicked += SelectCategory;

				genCategories.Add (b);
				genCategorySection.Add (b);
				}

			// Разблокировка
			topCategorySection.IsEnabled = true;
			}

		private void CategoriesRequest ()
			{
			categoriesReqResult = GMJ.GetCategories ((uint)lastTopCategoryIndex);
			}

		// Метод выполняет выбор категории и запрос записи, если возможно
		private void SelectCategory (object sender, EventArgs e)
			{
			// Контроль
			Button b = (Button)sender;
			int idx = genCategories.IndexOf (b);
			if (idx < 0)
				return;

			// Получение номера записи
			lastCategoryIndex = idx + (int)(currentCategoriesPage * categoriesPerPage);
			lastCategory = b.Text;

			// Запуск
			LastUsedCategory_Clicked (null, null);
			}

		// Метод запрашивает случайную запись из последней выбранной категории
		private void LastUsedCategory_Clicked (object sender, EventArgs e)
			{
			// Номер поста
			int post = GMJ.GetRandomFromCategory ((uint)lastCategoryIndex);
			if (post < 0)
				{
				if (lastCategoryIndex < 0)
					RDInterface.ShowBalloon ("Не выбрана категория для просмотра", true);
				else
					RDInterface.ShowBalloon ("Все записи из выбранной категории уже просмотрены", true);
				return;
				}

			// Запуск
			SetLogState (false);    // Блокировка автопрокрутки журнала
			RDInterface.MasterPage.PopToRootAsync (true);
			GMJ.RequestRecord ((uint)post);
			GetRecord (true);
			}

		// Выбор страницы подкатегорий
		private void ChangeCatPage_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				// Изменение значения
				Button b = (Button)sender;
				bool decrease = RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Backward);

				if (decrease)
					{
					if (currentCategoriesPage > 0)
						currentCategoriesPage--;
					else
						return;
					}
				else
					{
					if (currentCategoriesPage < categoriesPagesCount - 1)
						currentCategoriesPage++;
					else
						return;
					}

				// Перезагрузка списка
				if (lastTopCategoryIndex >= 0)
					SelectTopCategory (topCategories[lastTopCategoryIndex], null);
				}

			// Отображение
			uint end;
			if ((currentCategoriesPage + 1) * categoriesPerPage < categoriesReqResult.Length)
				end = (currentCategoriesPage + 1) * categoriesPerPage;
			else
				end = (uint)categoriesReqResult.Length;

			genCatCurrentPage.Text = (currentCategoriesPage * categoriesPerPage + 1).ToString () + " – " +
				end.ToString () + " из " + categoriesReqResult.Length.ToString ();
			}

		#endregion
		}
	}
