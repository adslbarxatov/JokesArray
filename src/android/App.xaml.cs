using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
		private int currentLogItem;

		// Управление центральной кнопкой журнала
		private bool centerButtonEnabled = true;

		// Сформированные контекстные меню
		private List<List<string>> tapMenuItems = [];
		private List<string> pageVariants = [];
		private List<string> pictureBKVariants = [];
		private List<string> pictureBKSelectionVariants = [];
		private List<string> pictureTXVariants = [];
		private List<string> pictureTAVariants = [];
		private List<string> pictureTASelectionVariants = [];
		private List<string> censorshipVariants = [];
		private List<string> logColorVariants = [];
		private List<string> logFontFamilyVariants = [];

		// Последняя использованная категория
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
			genCategoryEmpty, genCategoryLabel, genCatCurrentPage, topCategoryLabel,
			entryHeader, entryText, entrySign;

		private Switch keepScreenOnSwitch, enableCopySubscriptionSwitch,
			translucencySwitch, offlineModeSwitch;

		private Button centerButton, menuButton, sameCatButton,
			pictureBackButton, pictureTextButton, pTextOnTheLeftButton, censorshipButton, logColorButton,
			pSubsButton, logFontFamilyButton, genCatPrevPage, genCatNextPage,
			prevEntryButton, nextEntryButton, shareButton, heightButton;

		private List<Button> topCategories = [];
		private List<Button> genCategories = [];

		private FlexLayout topCategorySection, genCategorySection;
		private ScrollView entryScroll;

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
			DeviceDisplay.Current.MainDisplayInfoChanged += Current_MainDisplayInfoChanged;
			RDInterface.MasterPage.Popped += Current_LogPagePopped;

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
				RDDefaultButtons.Increase, settingsFieldBackColor, FontSizeButton_Clicked, true);
			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeDec",
				RDDefaultButtons.Decrease, settingsFieldBackColor, FontSizeButton_Clicked, true);
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
				aboutFieldBackColor, ReferenceButton_Click);

			Button hlp = RDInterface.ApplyButtonSettings (aboutPage, "HelpButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click);
			hlp.IsVisible = !RDGenerics.IsTV;

			Image qrImage = (Image)aboutPage.FindByName ("QRImage");
			qrImage.IsVisible = RDGenerics.IsTV;

			RDInterface.ApplyButtonSettings (aboutPage, "StatsButton",
				GMJ.GMJStatsMenuItem,
				aboutFieldBackColor, StatsButton_Click);

			/*RDInterface.ApplyLabelSettings (aboutPage, "HelpHeaderLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
				RDLabelTypes.HeaderLeft);
			Label htl = RDInterface.ApplyLabelSettings (aboutPage, "HelpTextLabel",
				RDGenerics.GetAppHelpText (), RDLabelTypes.SmallLeft);
			htl.TextType = TextType.Html;*/

			FontSizeButton_Clicked (null, null);

			#endregion

			#region Страница категорий

			// Категории верхнего уровня
			topCategoryLabel = RDInterface.ApplyLabelSettings (categoryPage, "TopCategoryLabel",
				"Группы категорий:", RDLabelTypes.HeaderLeft);
			topCategorySection = (FlexLayout)categoryPage.FindByName ("TopCategorySection");

			GMJLogColor currentLogColor = NotificationsSupport.LogColors.CurrentColor;
			string[] topCat = GMJ.GetTopCategories ();  // Активация списка
			for (int i = 0; i < topCat.Length; i++)
				{
				Button b = new Button ();
				RDInterface.ApplyButtonDefaults (b, true);

				b.BackgroundColor = currentLogColor.TranslucentColor;
				b.FontSize = 5 * RDInterface.MasterFontSize / 4;
				b.TextColor = currentLogColor.MainTextColor;
				b.WidthRequest = b.HeightRequest = RDInterface.MasterFontSize * 2.75;
				b.Padding = Thickness.Zero;
				b.Margin = new Thickness (3);
				b.Text = topCat[i];
				b.Clicked += SelectTopCategory;

				topCategories.Add (b);
				topCategorySection.Add (b);
				}

			genCategoryLabel = RDInterface.ApplyLabelSettings (categoryPage, "GenCategoryLabel",
				"Категории:", RDLabelTypes.HeaderLeft);
			genCategoryLabel.IsVisible = false;

			genCatPrevPage = RDInterface.ApplyButtonSettings (categoryPage, "GenCatPrevPage",
				RDDefaultButtons.Left, categoryFieldBackColor, ChangeCatPage_Clicked, true);
			genCatCurrentPage = RDInterface.ApplyLabelSettings (categoryPage, "GenCatCurrentPage",
				" ", RDLabelTypes.HeaderCenter);
			genCatNextPage = RDInterface.ApplyButtonSettings (categoryPage, "GenCatNextPage",
				RDDefaultButtons.Right, categoryFieldBackColor, ChangeCatPage_Clicked, true);
			genCatPrevPage.IsVisible = genCatNextPage.IsVisible = genCatCurrentPage.IsVisible = false;

			genCategorySection = (FlexLayout)categoryPage.FindByName ("GenCategorySection");
			genCategorySection.IsVisible = false;

			genCategoryEmpty = RDInterface.ApplyLabelSettings (categoryPage, "GenCategoryEmpty",
				"(все записи из этой категории уже просмотрены)", RDLabelTypes.TipCenter);
			genCategoryEmpty.IsVisible = false;

			#endregion

			#region Страницы журнала и настроек приложения

			entryScroll = (ScrollView)logPage.FindByName ("TextScroll");

			// Текстовое поле
			entryText = RDInterface.ApplyLabelSettings (logPage, "Text", " ", RDLabelTypes.DefaultLeft);
			entryText.HorizontalOptions = LayoutOptions.Fill;
			entryText.Margin = new Thickness (6);
			entryText.HorizontalTextAlignment = TextAlignment.Justify;

			// Поле заголовка
			entryHeader = RDInterface.ApplyLabelSettings (logPage, "Header", " ", RDLabelTypes.HeaderCenter);

			// Поле подписи
			entrySign = RDInterface.ApplyLabelSettings (logPage, "Sign", " ", RDLabelTypes.DefaultLeft);
			entrySign.HorizontalTextAlignment = TextAlignment.Center;
			entrySign.Margin = new Thickness (6);

			// Управление
			centerButton = RDInterface.ApplyButtonSettings (logPage, "CenterButton", "Ещё!",
				logFieldBackColor, CenterButton_Click,RDButtonFlags.BiggerFontSize);
			centerButton.FontAttributes = FontAttributes.Bold;
			centerButton.Padding = Thickness.Zero;

			prevEntryButton = RDInterface.ApplyButtonSettings (logPage, "PrevEntryButton",
				RDDefaultButtons.Left, logFieldBackColor, PrevEntry_Click, false);
			nextEntryButton = RDInterface.ApplyButtonSettings (logPage, "NextEntryButton",
				RDDefaultButtons.Right, logFieldBackColor, NextEntry_Click, false);
			centerButton.HeightRequest = centerButton.MaximumHeightRequest = prevEntryButton.HeightRequest;
			prevEntryButton.Padding = nextEntryButton.Padding = Thickness.Zero;

			// Главный журнал
			RDInterface.ApplyLabelSettings (settingsPage, "LogSettingsLabel",
				"Журнал", RDLabelTypes.HeaderLeft);

			// Цвет фона журнала
			RDInterface.ApplyLabelSettings (settingsPage, "LogColorLabel",
				"Цветовая тема:", RDLabelTypes.DefaultLeft);
			logColorButton = RDInterface.ApplyButtonSettings (settingsPage, "LogColorButton",
				" ", settingsFieldBackColor, LogColor_Clicked);
			RDInterface.ApplyLabelSettings (settingsPage, "LogColorTip",
				NotificationsSupport.LogColorTip, RDLabelTypes.TipJustify);

			// Кнопки меню и предложения в журнале
			menuButton = RDInterface.ApplyButtonSettings (logPage, "MenuButton", RDGenerics.IsTV ? "Меню" : "Зап.",
				logFieldBackColor, RDGenerics.IsTV ? Menu_Click : MainLogShare_Click, RDButtonFlags.None);
			menuButton.HeightRequest = menuButton.MaximumHeightRequest =
				menuButton.WidthRequest = menuButton.MaximumWidthRequest = prevEntryButton.HeightRequest;
			menuButton.Padding = Thickness.Zero;

			sameCatButton = RDInterface.ApplyButtonSettings (logPage, "SameCatButton", "Кат.",
				logFieldBackColor, LastUsedCategory_Clicked, RDButtonFlags.None);
			sameCatButton.HeightRequest = sameCatButton.MaximumHeightRequest =
				sameCatButton.WidthRequest = sameCatButton.MaximumWidthRequest =
				prevEntryButton.HeightRequest;
			sameCatButton.Padding = Thickness.Zero;

			shareButton = RDInterface.ApplyButtonSettings (logPage, "Share", RDDefaultButtons.Menu,
				aboutFieldBackColor, RDGenerics.IsTV ? null : Menu_Click, false);
			if (RDGenerics.IsTV)
				{
				shareButton.Text = " ";
				shareButton.IsEnabled = false;
				}
			shareButton.Padding = shareButton.Margin = Thickness.Zero;

			heightButton = RDInterface.ApplyButtonSettings (logPage, "Empty", RDDefaultButtons.UpDownArrow,
				aboutFieldBackColor, SwitchHeight_Click, false);
			heightButton.Padding = shareButton.Margin = Thickness.Zero;
			if (RDGenerics.IsTV)
				{
				heightButton.Text = " ";
				heightButton.IsEnabled = false;
				}

			// Режим полупрозрачности
			RDInterface.ApplyLabelSettings (settingsPage, "TranslucencyLabel",
				"Светлый фон для текстов записей", RDLabelTypes.DefaultLeft);
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
				RDDefaultButtons.Increase, settingsFieldBackColor, LogFontSizeChanged, true);
			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeDecButton",
				RDDefaultButtons.Decrease, settingsFieldBackColor, LogFontSizeChanged, true);

			RDInterface.ApplyLabelSettings (settingsPage, "FontSizeFieldTip",
				NotificationsSupport.FontSizeFieldTip, RDLabelTypes.TipJustify);

			LogFontSizeChanged (null, null);

			// Размер группы запрашиваемых записей
			groupSizeFieldLabel = RDInterface.ApplyLabelSettings (settingsPage, "GroupSizeFieldLabel",
				"", RDLabelTypes.DefaultLeft);
			groupSizeFieldLabel.TextType = TextType.Html;

			RDInterface.ApplyButtonSettings (settingsPage, "GroupSizeIncButton",
				RDDefaultButtons.Increase, settingsFieldBackColor, GroupSizeChanged, true);
			RDInterface.ApplyButtonSettings (settingsPage, "GroupSizeDecButton",
				RDDefaultButtons.Decrease, settingsFieldBackColor, GroupSizeChanged, true);
			RDInterface.ApplyLabelSettings (settingsPage, "GroupSizeFieldTip",
				NotificationsSupport.GroupSizeFieldTip, RDLabelTypes.TipJustify);

			GroupSizeChanged (null, null);

			// Цензурирование
			censorshipButton = RDInterface.ApplyButtonSettings (settingsPage, "CensorshipButton",
				" ", settingsFieldBackColor, Censorship_Clicked);

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
				" ", settingsFieldBackColor, LogFontFamily_Clicked);
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
				" ", settingsFieldBackColor, PictureBack_Clicked);
			Label pictBackLabel2 = RDInterface.ApplyLabelSettings (settingsPage, "PicturesBackTip",
				NotificationsSupport.PicturesBackTip, RDLabelTypes.TipJustify);

			// Текст и рамка картинок
			Label pictTextLabel1 = RDInterface.ApplyLabelSettings (settingsPage, "PicturesTextLabel",
				"Текст и рамка:", RDLabelTypes.DefaultLeft);
			pictureTextButton = RDInterface.ApplyButtonSettings (settingsPage, "PicturesTextButton",
				" ", settingsFieldBackColor, PictureText_Clicked);
			Label pictTextLabel2 = RDInterface.ApplyLabelSettings (settingsPage, "PicturesTextTip",
				NotificationsSupport.PicturesTextTip, RDLabelTypes.TipJustify);

			// Выравнивание текста
			Label pictTextAlignLabel1 = RDInterface.ApplyLabelSettings (settingsPage, "PTextLeftLabel",
				"Выравнивание:", RDLabelTypes.DefaultLeft);
			pTextOnTheLeftButton = RDInterface.ApplyButtonSettings (settingsPage, "PTextLeftButton",
				" ", settingsFieldBackColor, PTextOnTheLeft_Toggled);
			Label pictTextAlignLabel2 = RDInterface.ApplyLabelSettings (settingsPage, "PTextLeftTip",
				NotificationsSupport.PicturesTextAlignmentTip, RDLabelTypes.TipJustify);

			// Подпись картинок
			Label pictSubsLabel1 = RDInterface.ApplyLabelSettings (settingsPage, "PSubsLabel",
				"Подпись:", RDLabelTypes.DefaultLeft);
			pSubsButton = RDInterface.ApplyButtonSettings (settingsPage, "PSubsButton",
				" ", settingsFieldBackColor, PSubs_Clicked);
			Label pictSubsLabel2 = RDInterface.ApplyLabelSettings (settingsPage, "PSubsTip",
				NotificationsSupport.PicturesSubscriptionTip, RDLabelTypes.TipJustify);

			if (RDGenerics.IsTV)
				{
				pictLabel.IsVisible =
					pictBackLabel1.IsVisible = pictBackLabel2.IsVisible = pictureBackButton.IsVisible =
					pictTextLabel1.IsVisible = pictTextLabel2.IsVisible = pictureTextButton.IsVisible =
					pictTextAlignLabel1.IsVisible = pictTextAlignLabel2.IsVisible = pTextOnTheLeftButton.IsVisible =
					pictSubsLabel1.IsVisible = pictSubsLabel2.IsVisible = pSubsButton.IsVisible = false;
				}
			else
				{
				PictureBack_Clicked (null, null);
				PictureText_Clicked (null, null);
				PTextOnTheLeft_Toggled (null, null);
				PSubs_Clicked (null, null);
				}

			#endregion

			// Первичная загрузка журнала
			SetLogState (false);
			UpdateLogButton (true, true);

			// Перезапрос журнала
			masterLog = new List<MainLogItem> (NotificationsSupport.GetMasterLog (true));
			currentLogItem = masterLog.Count - 1;

			UpdateLog ();
			SetLogState (true);

			// Принятие соглашений
			ShowStartupTips ();
			return mainPage;
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
					"• Используйте кнопку «" + centerButton.Text + "» для получения случайных записей из архива",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Next));

				if (RDGenerics.IsTV)
					{
					await RDInterface.ShowMessage ("Внимание!" + RDLocale.RNRN +
						"• Убедитесь, что данное устройство имеет выход в интернет. Если такой возможности нет, " +
						"не забудьте включить оффлайн-режим в настройках приложения." + RDLocale.RNRN +
						"• Ознакомьтесь с описанием проекта в разделе «О приложении» (кнопка «" +
						(RDGenerics.IsTV ? shareButton.Text : menuButton.Text) + "»). Убедитесь, " +
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
		private async Task<bool> ShowTips (NSTipTypes Type)
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
					msg = "Все операции с текстами записей доступны по кнопке «" + menuButton.Text +
						"» в углу страницы";
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
		/// Запуск интерфейса
		/// </summary>
		protected override void OnStart ()
			{
			Current_MainDisplayInfoChanged (null, null);
			base.OnStart ();
			}

		/// <summary>
		/// Возврат в интерфейс при сворачивании
		/// </summary>
		protected override void OnResume ()
			{
			RDInterface.MasterPage.PopToRootAsync (true);

			Current_MainDisplayInfoChanged (null, null);
			base.OnResume ();
			}

		/// <summary>
		/// Возврат в интерфейс из статичного оповещения (использует перенаправление в MasterPage)
		/// </summary>
		public void ResumeApp ()
			{
			OnResume ();
			}

		// Изменение ориентации экрана
		private async void Current_MainDisplayInfoChanged (object sender, DisplayInfoChangedEventArgs e)
			{
			await Task.Delay (500);

			entryScroll.HeightRequest = entryScroll.MaximumHeightRequest = logPage.Height - shareButton.Height - 
				11 * centerButton.Height / 8 - NotificationsSupport.AdditionalLogHeight;
			}

		// Этот вызов необходим для корректной разметки страницы журнала, когда первой отображается страница настроек
		private async void Current_LogPagePopped (object sender, NavigationEventArgs e)
			{
			Current_MainDisplayInfoChanged (null, null);
			}

		#endregion

		#region Журнал

		// Принудительное обновление лога
		private void UpdateLog ()
			{
			if (masterLog.Count < 1)
				return;

			MainLogItem item = masterLog[currentLogItem];

			entryHeader.Text = item.Header;
			entryText.Text = item.Text;
			entrySign.Text = item.Separator;
			entrySign.HorizontalOptions = item.SeparatorAlignment;
			}

		// Обновление формы кнопки журнала
		private void UpdateLogButton (bool Requesting, bool FinishingBackgroundRequest)
			{
			bool red = Requesting && FinishingBackgroundRequest;
			bool yellow = Requesting && !FinishingBackgroundRequest;
			bool dark = !NotificationsSupport.LogColors.CurrentColor.IsBright;

			if (red)
				centerButton.TextColor = Color.FromArgb (dark ? "#FF4040" : "#D00000");
			else if (yellow)
				centerButton.TextColor = Color.FromArgb (dark ? "#FFFF40" : "#D0D000");
			else
				centerButton.TextColor = Color.FromArgb (dark ? "#40FF40" : "#00D000");
			}

		// Выбор оповещения для перехода или share
		private async void MainLogShare_Click (object sender, EventArgs e)
			{
			// Контроль
			if (RDGenerics.IsTV)
				return;

			MainLogItem notItem = masterLog[currentLogItem];
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
			const string copyTextName = "📑\t Скопировать текст";
			const string shareTextName = "📣\t Поделиться текстом";
			const string originalName = "➡️\t Перейти к оригиналу";
			if (tapMenuItems.Count < 1)
				{
				tapMenuItems.Add ([
					shareTextName,
					copyTextName,
					]);
				tapMenuItems.Add ([
					originalName,
					shareTextName,
					copyTextName,
					]);
				tapMenuItems.Add ([
					originalName,
					shareTextName,
					"🌅\t Поделиться картинкой",
					copyTextName,
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
						{
						if (await RDInterface.ShowMessage (GMJ.CensorshipGoToChannelMessage,
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK),
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel)))
							{
							await RDGenerics.RunURL (notLink, true);
							}
						}
					break;

				// Поделиться
				case 10:
				case 21:
				case 31:
					if (!((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.ShareTextButton))
						await ShowTips (NSTipTypes.ShareTextButton);

					await Share.RequestAsync (notText, RDGenerics.DefaultAssemblyVisibleName);
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
				}

			// Завершено
			}

		// Переключение ограничителя высоты журнала
		private async void SwitchHeight_Click (object sender, EventArgs e)
			{
			if (NotificationsSupport.AdditionalLogHeight != 0)
				NotificationsSupport.AdditionalLogHeight = 0;
			else
				NotificationsSupport.AdditionalLogHeight = (uint)centerButton.Height;

			Current_MainDisplayInfoChanged (null, null);
			}

		// Блокировка / разблокировка кнопок
		private void SetLogState (bool State)
			{
			// Переключение состояния кнопок и свичей
			centerButtonEnabled = State;
			menuButton.IsVisible = sameCatButton.IsVisible = State;

			if (!State)
				{
				prevEntryButton.IsVisible = nextEntryButton.IsVisible = false;
				heightButton.IsEnabled = shareButton.IsEnabled = false;
				}
			else
				{
				UpdateNavButtons ();
				heightButton.IsEnabled = shareButton.IsEnabled = !RDGenerics.IsTV;
				}

			// Обновление статуса
			UpdateLogButton (!State, false);
			}

		// Добавление текста в журнал
		private void AddTextToLog (string Text)
			{
			masterLog.Add (new MainLogItem (Text));

			// Удаление верхних строк
			while (masterLog.Count > NotificationsSupport.MasterLogMaxItems)
				masterLog.RemoveAt (0);

			currentLogItem = masterLog.Count - 1;
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
					AddTextToLog (newText);
					UpdateLog ();

					// Завершено
					success = true;
					}
				}

			// Разблокировка
			SetLogState (true);
			UpdateLogButton (!success, !success);
			if (!RDGenerics.IsTV && !((NSTipTypes)RDGenerics.TipsState).HasFlag (NSTipTypes.MainLogClickMenuTip))
				await ShowTips (NSTipTypes.MainLogClickMenuTip);
			}

		// Переход между страницами журнала
		private void PrevEntry_Click (object sender, EventArgs e)
			{
			currentLogItem--;
			UpdateLog ();
			UpdateNavButtons ();
			}

		private void NextEntry_Click (object sender, EventArgs e)
			{
			currentLogItem++;
			UpdateLog ();
			UpdateNavButtons ();
			}

		private void UpdateNavButtons ()
			{
			nextEntryButton.IsVisible = (currentLogItem < masterLog.Count - 1);
			prevEntryButton.IsVisible = (currentLogItem > 0);
			}

		// Выбор текущей страницы
		private async void Menu_Click (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pageVariants.Count < 1)
				{
				pageVariants = [
					"▶️\t Ещё!",
					"🔄\t Та же категория",
					"🔍\t Все категории",
					"⚙️\t Настройки приложения",
					"ℹ️\t " + RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
					];

				if (!RDGenerics.IsTV)
					pageVariants.Add ("🆕\t Предложить запись");
				}

			int res;
			if (sender == null)
				{
				res = 2;
				}
			else
				{
				res = await RDInterface.ShowList (RDLocale.GetDefaultText (RDLDefaultTexts.Button_GoTo),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pageVariants);
				if (res < 0)
					return;
				}

			// Вызов
			switch (res)
				{
				case 0:
					CenterButton_Click (null, null);
					break;

				case 1:
					LastUsedCategory_Clicked (null, null);
					break;

				case 2:
					if (GMJ.RecordsLeft < 1)
						{
						await RDInterface.ShowMessage ("Архив записей ещё не обновлялся." + RDLocale.RNRN +
							"Нажмите кнопку «" + centerButton.Text + "», чтобы запросить первую запись из архива",
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
						return;
						}

					RDInterface.SetCurrentPage (categoryPage, categoryMasterBackColor);

					if (lastTopCategoryIndex >= 0)
						SelectTopCategory (topCategories[lastTopCategoryIndex], null);
					break;

				case 3:
					RDInterface.SetCurrentPage (settingsPage, settingsMasterBackColor);
					break;

				case 4:
					RDInterface.SetCurrentPage (aboutPage, aboutMasterBackColor);
					break;

				case 5:
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

		// Изменение размера шрифта лога
		private void LogFontSizeChanged (object sender, EventArgs e)
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

			// Применение к журналу
			entrySign.FontSize = NotificationsSupport.LogFontSize * 0.7;
			entryText.FontSize = NotificationsSupport.LogFontSize;
			}

		// Изменение количества записей, запрашиваемых подряд
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

		// Выбор цвета текста картинок
		private async void PictureText_Clicked (object sender, EventArgs e)
			{
			// Запрос варианта
			if (pictureTXVariants.Count < 1)
				pictureTXVariants.AddRange (GMJPicture.PictureTextColorNames);

			int res;
			if (sender == null)
				{
				res = (int)NotificationsSupport.PicturesTextColor;
				}
			else
				{
				res = await RDInterface.ShowList ("Цвет текста картинок",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), pictureTXVariants);
				if (res < 0)
					return;

				NotificationsSupport.PicturesTextColor = (GMJPictureTextColor) res;
				}

			pictureTextButton.Text = pictureTXVariants[res];
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

			// Цвета журнала
			logPage.BackgroundColor = centerButton.BackgroundColor =
				prevEntryButton.BackgroundColor = nextEntryButton.BackgroundColor =
				menuButton.BackgroundColor = sameCatButton.BackgroundColor = logColorButton.BackgroundColor =
				shareButton.BackgroundColor = heightButton.BackgroundColor = currentLogColor.BackColor;

			logColorButton.TextColor = currentLogColor.MainTextColor;
			menuButton.TextColor = sameCatButton.TextColor = prevEntryButton.TextColor =
				nextEntryButton.TextColor = shareButton.TextColor = heightButton.TextColor =
				currentLogColor.SecondaryTextColor;

			if (NotificationsSupport.TranslucentLogItems)
				entryScroll.BackgroundColor = currentLogColor.TranslucentColor;
			else
				entryScroll.BackgroundColor = currentLogColor.BackColor;

			entryText.TextColor = currentLogColor.MainTextColor;
			entryHeader.TextColor = currentLogColor.SecondaryTextColor;
			entrySign.TextColor = currentLogColor.MainTextColor;

			// Цвета раздела категорий
			categoryPage.BackgroundColor = currentLogColor.BackColor;
			topCategoryLabel.TextColor = genCategoryLabel.TextColor = genCatCurrentPage.TextColor =
				genCatPrevPage.TextColor = genCatNextPage.TextColor = currentLogColor.MainTextColor;

			for (int i = 0; i < topCategories.Count; i++)
				{
				topCategories[i].BackgroundColor = currentLogColor.TranslucentColor;
				topCategories[i].TextColor = currentLogColor.MainTextColor;
				}

			genCatPrevPage.BackgroundColor = genCatNextPage.BackgroundColor = currentLogColor.TranslucentColor;
			for (int i = 0; i < genCategories.Count; i++)
				{
				genCategories[i].BackgroundColor = currentLogColor.TranslucentColor;
				genCategories[i].TextColor = currentLogColor.MainTextColor;
				}

			genCategoryEmpty.TextColor = currentLogColor.SecondaryTextColor;

			// Навигатор страницы
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

			// Цепляет кнопку журнала
			UpdateLogButton (false, false);
			}

		private void Translucency_Toggled (object sender, EventArgs e)
			{
			NotificationsSupport.TranslucentLogItems = translucencySwitch.IsToggled;

			GMJLogColor currentLogColor = NotificationsSupport.LogColors.CurrentColor;
			if (NotificationsSupport.TranslucentLogItems)
				entryScroll.BackgroundColor = currentLogColor.TranslucentColor;
			else
				entryScroll.BackgroundColor = currentLogColor.BackColor;
			}

		// Изменение размера и семейства шрифта лога
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
			entryText.FontAttributes = fa;
			entryText.FontFamily = ff;

			entrySign.FontAttributes = FontAttributes.Italic;
			entrySign.FontFamily = ff;
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
			genCatPrevPage.IsVisible = genCatNextPage.IsVisible = false;

			// Запрос
			lastTopCategoryIndex = idx;
			await Task.Run (CategoriesRequest);

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

			GMJLogColor currentLogColor = NotificationsSupport.LogColors.CurrentColor;
			for (int i = (int)(currentCategoriesPage * categoriesPerPage);
				(i < (currentCategoriesPage + 1) * categoriesPerPage) && (i < categoriesReqResult.Length); i++)
				{
				Button b = new Button ();
				RDInterface.ApplyButtonDefaults (b, true);
				b.LineBreakMode = LineBreakMode.NoWrap;	// Именно здесь это критично

				b.BackgroundColor = currentLogColor.TranslucentColor;
				b.FontSize = RDInterface.MasterFontSize;
				b.HeightRequest = RDInterface.MasterFontSize * 2.75;
				b.MinimumWidthRequest = b.HeightRequest;
				b.TextColor = currentLogColor.MainTextColor;
				b.Margin = new Thickness (3);
				b.Padding = new Thickness (6, 0);
				b.Text = " " + categoriesReqResult[i] + " ";
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
				Menu_Click (null, null);
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
				bool decrease = RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Left);

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
