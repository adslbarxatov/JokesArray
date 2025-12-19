using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму приложения
	/// </summary>
	public partial class GrammarMustJoyForm: Form
		{
		// Переменные
		private NotifyIcon ni = new NotifyIcon ();
		private bool allowExit = false;
		private bool hideWindow;

		private char[] groupSplitter = ['\x1'];

		private ContextMenuStrip textContextMenu;
		private int textContextSender;

		private const string fontFamily = "Calibri";
		private const int translucencyAmount = 15;

		// Контекстные меню категорий
		private ContextMenuStrip topCategories;
		private ContextMenuStrip genCategories;

		// Последняя использованная категория
		private int lastCategoryIndex = -1;
		private int lastTopCategoryIndex = -1;
		private bool fromCategory = false;
		private const int categoriesPerMenu = 20;

		// Список полученных категорий
		private string[] categoriesReqResult;

		// Динамическое ограничение ширины элементов журнала
		private Size LogSizeLimit
			{
			get
				{
				return new Size (MainLayout.Width - 6 - 18, 0);
				}
			}

		// Динамический внешний отступ элементов журнала
		private static Padding LogItemMargin
			{
			get
				{
				return new Padding (3, 3, 3, (int)NotificationsSupport.LogFontSize /
					(NotificationsSupport.TranslucentLogItems ? 8 : 4));
				}
			}

		/// <summary>
		/// Конструктор. Запускает главную форму приложения
		/// </summary>
		public GrammarMustJoyForm (bool HideWindow)
			{
			// Инициализация
			InitializeComponent ();

			/*this.Text = ProgramDescription.AssemblyVisibleName;*/
			this.Text = RDGenerics.DefaultAssemblyVisibleName;
			this.CancelButton = BClose;
			if (!RDGenerics.AppHasAccessRights (false, false))
				this.Text += RDLocale.GetDefaultText (RDLDefaultTexts.Message_LimitedFunctionality);
			hideWindow = HideWindow;

			// Принудительные параметры
			if (!RDLocale.IsCurrentLanguageRuRu)
				RDLocale.CurrentLanguage = RDLanguages.ru_ru;

			// Получение настроек
			RDGenerics.LoadWindowDimensions (this);
			ApplyColorsAndFonts ();

			// Настройка иконки в трее
			ni.Icon = JokesArrayResources.TrayIcon;
			/*ni.Text = ProgramDescription.AssemblyVisibleName;*/
			ni.Text = RDGenerics.DefaultAssemblyVisibleName;
			ni.Visible = true;

			ni.ContextMenuStrip = new ContextMenuStrip ();
			ni.ContextMenuStrip.ShowImageMargin = false;
			ni.ContextMenuStrip.Items.Add (GMJ.GMJStatsMenuItem, null, BHelp_ItemClicked);
			ni.ContextMenuStrip.Items.Add ("Настройки", null, BHelp_ItemClicked);
			ni.ContextMenuStrip.Items.Add (RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
				null, BHelp_ItemClicked);
			ni.ContextMenuStrip.Items.Add (RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit),
				null, BHelp_ItemClicked);

			ni.MouseDown += ShowHideFullText;

			// Цензурирование
			if (RDGenerics.StartedFromMSStore)
				{
				if (!GMJ.EnableCensorship)
					GMJ.EnableCensorship = true;
				}

			// Контекстное меню журнала
			textContextMenu = new ContextMenuStrip ();
			textContextMenu.ShowImageMargin = false;
			textContextMenu.Items.Add ("Копировать текст", null, TextContext_ItemClicked);
			textContextMenu.Items.Add ("Сохранить картинку", null, TextContext_ItemClicked);

			// Окно сохранения картинок
			SFDialog.Title = "Укажите расположение для сохраняемой картинки";
			SFDialog.Filter = "Portable network graphics (*.png)|*.png";

			// Загрузка категорий верхнего уровня
#if TGB
			LastCategoryButton.Enabled = false;
#else
			string[] topCat = GMJ.GetTopCategories ();
			topCategories = new ContextMenuStrip ();
			topCategories.ShowImageMargin = false;
			genCategories = new ContextMenuStrip ();
			genCategories.ShowImageMargin = false;

			for (int i = 0; i < topCat.Length; i++)
				topCategories.Items.Add (topCat[i].ToUpper (), null, SelectTopCategory);

#endif
			}

		private void GrammarMustJoyForm_Shown (object sender, EventArgs e)
			{
			// Скрытие окна настроек
			GrammarMustJoyForm_Resize (null, null);
			if (hideWindow)
				this.Hide ();
			}

		// Закрытие окна
		private void GrammarMustJoyForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			// Остановка службы
			if (allowExit)
				{
				// Остановка
				ni.Visible = false;
				}

			// Скрытие окна просмотра
			else
				{
				this.Hide ();
				e.Cancel = true;
				}
			}

		// Отображение / скрытие полного списка оповещений
		private void ShowHideFullText (object sender, MouseEventArgs e)
			{
			// Работа только с левой кнопкой мыши
			if (e.Button != MouseButtons.Left)
				return;

			// Обработка состояния
			if (this.Visible)
				{
				this.Close ();
				}
			else
				{
				this.Show ();
				this.TopMost = true;
				this.TopMost = false;
				ScrollLog ();
				}
			}

		// Метод прокручивает журнал к последней записи
		private void ScrollLog ()
			{
			if (MainLayout.Controls.Count > 0)
				MainLayout.ScrollControlIntoView (MainLayout.Controls[MainLayout.Controls.Count - 1]);
			}

		// Закрытие окна просмотра
		private void BClose_Click (object sender, EventArgs e)
			{
			this.Close ();
			}

		// Загрузка параметров после настройки
		private void ApplyColorsAndFonts ()
			{
			// Извлечение индекса
			Font fnt = new Font (fontFamily, NotificationsSupport.LogFontSize / 10.0f);

			MainLayout.BackColor = NotificationsSupport.LogColors.CurrentColor.BackColor;
			for (int i = 0; i < MainLayout.Controls.Count; i++)
				{
				Label l = (Label)MainLayout.Controls[i];
				l.Font = fnt;
				l.Margin = LogItemMargin;

				int amount = NotificationsSupport.TranslucentLogItems ? translucencyAmount : 0;
				if (NotificationsSupport.LogColors.CurrentColor.IsBright)
					l.BackColor = Color.FromArgb (amount, 0, 0, 0);
				else
					l.BackColor = Color.FromArgb (amount, 255, 255, 255);

				l.ForeColor = NotificationsSupport.LogColors.CurrentColor.MainTextColor;
				}
			}

		// Изменение размера формы
		private void GrammarMustJoyForm_Resize (object sender, EventArgs e)
			{
			MainLayout.Width = this.Width - 38;
			MainLayout.Height = this.Height - ButtonsPanel.Height - 53;

			ButtonsPanel.Top = MainLayout.Top + MainLayout.Height + 1;
			ButtonsPanel.Left = (this.Width - ButtonsPanel.Width) / 2;
			}

		// Сохранение размера формы
		private void GrammarMustJoyForm_ResizeEnd (object sender, EventArgs e)
			{
			// Сохранение настроек
			RDGenerics.SaveWindowDimensions (this);
			// Пересчёт размеров элементов
			for (int i = 0; i < MainLayout.Controls.Count; i++)
				{
				Label l = (Label)MainLayout.Controls[i];
				l.AutoSize = false;
				l.MaximumSize = l.MinimumSize = LogSizeLimit;
				l.AutoSize = true;
				}
			}

		// Запрос сообщения от JokesArray
		private void GetJokeExecutor (object sender, DoWorkEventArgs e)
			{
			uint group = NotificationsSupport.GroupSize;
			BackgroundWorker bw = (BackgroundWorker)sender;
			string res = "";
			string sp = groupSplitter[0].ToString ();
			string limit = " из " + group.ToString ();

			for (int i = 0; i < group; i++)
				{
				// Антиспам
				if (i > 0)
					Thread.Sleep (1000);

				res += (GMJ.GetRandomRecord () + sp);
				bw.ReportProgress ((int)(RDWorkerForm.ProgressBarSize * (i + 1) / group),
					"Запрошено " + (i + 1).ToString () + limit);
				}

			e.Result = res;
			}

		private void GetJoke_Click (object sender, EventArgs e)
			{
			// Запрос записи
			RDInterface.RunWork (GetJokeExecutor, null, fromCategory ? "Запрос записи..." : "Запрос случайной записи...",
				RDRunWorkFlags.CaptionInTheMiddle);
			fromCategory = false;

			string[] values = RDInterface.WorkResultAsString.Split (groupSplitter,
				StringSplitOptions.RemoveEmptyEntries);

			if (values.Length < 1)
				{
				AddTextToLayout (GMJ.NoConnectionPattern);
				}
			else
				{
				for (int i = 0; i < values.Length; i++)
					AddTextToLayout (values[i].Replace (NotificationsSupport.MainLogItemSplitter.ToString (),
						RDLocale.RN));
				}
			}

		// Метод добавляет этемент в MainLayout
		private void AddTextToLayout (string Text)
			{
			// Формирование контрола
			Label l = new Label ();
			l.AutoSize = false;
			bool err = Text.StartsWith (GMJ.NoConnectionPattern) || Text.StartsWith (GMJ.SourceNoReturnPattern);

			int amount = NotificationsSupport.TranslucentLogItems ? translucencyAmount : 0;
			if (err)
				l.BackColor = RDInterface.GetInterfaceColor (RDInterfaceColors.WarningMessage);
			else if (NotificationsSupport.LogColors.CurrentColor.IsBright)
				l.BackColor = Color.FromArgb (amount, 0, 0, 0);
			else
				l.BackColor = Color.FromArgb (amount, 255, 255, 255);

			l.MouseClick += TextLabel_MouseClicked;
			l.Font = new Font (fontFamily, NotificationsSupport.LogFontSize / 10.0f);

			if (err)
				l.ForeColor = RDInterface.GetInterfaceColor (RDInterfaceColors.ErrorText);
			else
				l.ForeColor = NotificationsSupport.LogColors.CurrentColor.MainTextColor;

			l.Text = Text;
			l.Margin = LogItemMargin;

			l.MaximumSize = l.MinimumSize = LogSizeLimit;
			l.AutoSize = true;

			// Добавление
			MainLayout.Controls.Add (l);

			while (MainLayout.Controls.Count > NotificationsSupport.MasterLogMaxItems)
				MainLayout.Controls.RemoveAt (0);

			// Прокрутка
			ScrollLog ();
			}

		// Изменение размера шрифта
		// Нажатие на элемент журнала
		private void TextLabel_MouseClicked (object sender, MouseEventArgs e)
			{
			Label l = (Label)sender;
			textContextSender = MainLayout.Controls.IndexOf (l);

			// Контроль возможности формирования картинки
			textContextMenu.Items[1].Enabled = !l.Text.Contains (GMJ.SourceNoReturnPattern) &&
				!l.Text.Contains (GMJ.NoConnectionPattern) && (GMJPicture.AlignString (l.Text) != null);

			textContextMenu.Show (l, e.Location);
			}

		// Выбор варианта в меню элемента в журнале
		private void TextContext_ItemClicked (object sender, EventArgs e)
			{
			int idx = textContextMenu.Items.IndexOf ((ToolStripItem)sender);
			if (textContextSender < 0)
				return;
			Label l = (Label)MainLayout.Controls[textContextSender];

			switch (idx)
				{
				// Копировать в буфер
				case 0:
					string notItem = l.Text;
					string notLink = "";

					int left = notItem.IndexOf (GMJ.NumberStringBeginning);
					int right;
					if (left >= 0)
						right = notItem.IndexOf (GMJ.NumberStringEnding, left);
					else
						right = left;

					if (right >= 0)
						{
						left += GMJ.NumberStringBeginning.Length;
						notLink = GMJ.SourceRedirectLink + "/" + notItem.Substring (left, right - left);
						}

					bool add = GMJ.EnableCopySubscription && !string.IsNullOrWhiteSpace (notLink);
					RDGenerics.SendToClipboard (notItem + (add ? (RDLocale.RNRN + notLink) : ""), true);
					break;

				// Сохранить картинку
				case 1:
					// Разделение на компоненты
					int hSize = l.Text.IndexOf (RDLocale.RN);
					string header = l.Text.Substring (0, hSize);
					string text = l.Text.Substring (hSize + RDLocale.RNRN.Length);

					string sub = "";
					if (text.EndsWith (']'))
						{
						int sSize = text.LastIndexOf (RDLocale.RNRN);
						sub = text.Substring (sSize + RDLocale.RNRN.Length);

						sub = sub.Substring (1, sub.Length - 2);
						text = text.Substring (0, sSize);
						}

					int pbk;
					switch (NotificationsSupport.PicturesBackgroundType)
						{
						case NotificationsSupport.PicturesBackgroundRandom:
							pbk = RDGenerics.RND.Next (NotificationsSupport.PictureColors.ColorNames.Length);
							break;

						default:
							pbk = NotificationsSupport.PicturesBackgroundType;
							break;
						}

					// Создание изображения
					Bitmap b = GMJPicture.CreateRecordPicture (header, text, sub,
						NotificationsSupport.PicturesTextAlignment,
						NotificationsSupport.PictureColors.GetColor ((uint)pbk));

					// Сохранение
					SFDialog.FileName = GMJPicture.GetFileNameFromCode (header);

					if (SFDialog.ShowDialog () == DialogResult.OK)
						GMJPicture.SaveRecordPictureToFile (b, SFDialog.FileName);

					b.Dispose ();
					break;
				}
			}

		// Выбор варианта в меню иконки в трее
		private void BHelp_ItemClicked (object sender, EventArgs e)
			{
			// Извлечение индекса
			int idx = ni.ContextMenuStrip.Items.IndexOf ((ToolStripItem)sender);

			// Вызов
			switch (idx)
				{
				case 0:
					RDInterface.MessageBox (RDMessageFlags.Information | RDMessageFlags.NoSound, GMJ.GMJStats);
					break;

				case 1:
					BSettings_Click (null, null);
					break;

				case 2:
					RDInterface.ShowAbout (false);
					break;

				case 3:
					allowExit = true;
					this.Close ();
					break;
				}
			}

		// Предложение записей
		private void BAdd_Click (object sender, EventArgs e)
			{
			if (RDInterface.MessageBox (RDMessageFlags.Question | RDMessageFlags.CenterText, GMJ.SuggestionMessage,
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)) != RDMessageButtons.ButtonOne)
				return;

			RDAboutForm.AskDeveloper ();
			}

		// Вызов настроек
		private void BSettings_Click (object sender, EventArgs e)
			{
			GMJSettingsForm gmjsf = new GMJSettingsForm ();
			gmjsf.Dispose ();

			ApplyColorsAndFonts ();
			}

		// Метод выполняет выбор категории верхнего уровня
		private void SelectTopCategory_Clicked (object sender, EventArgs e)
			{
#if TGB
			GMJ.AddNewIndexes ();
#else
			topCategories.Show (LastCategoryButton, Point.Empty);
#endif
			}

		private void SelectTopCategory (object sender, EventArgs e)
			{
			// Сброс списков
			genCategories.Items.Clear ();

			// Запрос доступных категорий
			int idx = topCategories.Items.IndexOf ((ToolStripItem)sender);
			if (idx < 0)
				return;

			// Запрос
			lastTopCategoryIndex = idx;
			RDInterface.RunWork (CategoriesRequest, null, "Загрузка категорий...", RDRunWorkFlags.CaptionInTheMiddle);

			// Контроль
			if (categoriesReqResult.Length < 1)
				{
				RDInterface.MessageBox (RDMessageFlags.Information | RDMessageFlags.CenterText,
					"Все записи из этой категории уже просмотрены", 1000);
				return;
				}

			// Загрузка
			if (categoriesReqResult.Length <= categoriesPerMenu)
				{
				for (int i = 0; i < categoriesReqResult.Length; i++)
					genCategories.Items.Add (categoriesReqResult[i], null, SelectCategory);
				genCategories.Tag = (int)0;
				}
			else
				{
				for (int i = 0; i < categoriesReqResult.Length; i += categoriesPerMenu)
					{
					string left = (i < categoriesReqResult.Length) ? categoriesReqResult[i] :
						categoriesReqResult[categoriesReqResult.Length - 1];
					string right = (i + categoriesPerMenu - 1 < categoriesReqResult.Length) ?
						categoriesReqResult[i + categoriesPerMenu - 1] :
						categoriesReqResult[categoriesReqResult.Length - 1];
					if (left != right)
						left += (" – " + right);

					genCategories.Items.Add (left, null, SelectCategoryGroup);
					}
				}

			// Запуск второго меню
			genCategories.Show (LastCategoryButton, Point.Empty);
			}

		private void CategoriesRequest (object sender, DoWorkEventArgs e)
			{
#if !TGB
			categoriesReqResult = GMJ.GetCategories ((uint)lastTopCategoryIndex);
#endif
			}

		// Метод выполняет выбор категории и запрос записи, если возможно
		private void SelectCategoryGroup (object sender, EventArgs e)
			{
			// Контроль
			ToolStripItem b = (ToolStripItem)sender;
			int offset = genCategories.Items.IndexOf (b) * categoriesPerMenu;
			if (offset < 0)
				return;

			genCategories.Items.Clear ();
			for (int i = offset; (i < offset + categoriesPerMenu) && (i < categoriesReqResult.Length); i++)
				genCategories.Items.Add (categoriesReqResult[i], null, SelectCategory);

			// Запуск третьего меню
			genCategories.Tag = offset;

			if (genCategories.Items.Count < 2)
				SelectCategory (genCategories.Items[0], null);  // Напрямую, т.к. остался один вариант
			else
				genCategories.Show (LastCategoryButton, Point.Empty);
			}

		private void SelectCategory (object sender, EventArgs e)
			{
			// Контроль
			ToolStripItem b = (ToolStripItem)sender;
			int idx = genCategories.Items.IndexOf (b) + (int)genCategories.Tag;
			if (idx < 0)
				return;

			// Получение номера записи
			lastCategoryIndex = idx;

			// Запуск
			LastUsedCategory_Clicked (null, null);
			}

		// Метод запрашивает случайную запись из последней выбранной категории
		private void LastUsedCategory_Clicked (object sender, EventArgs e)
			{
#if !TGB
			// Номер поста
			int post = GMJ.GetRandomFromCategory ((uint)lastCategoryIndex);
			if (post < 0)
				{
				RDInterface.MessageBox (RDMessageFlags.Information | RDMessageFlags.CenterText,
					(lastCategoryIndex < 0) ? "Не выбрана категория для просмотра" :
					"Все записи из выбранной категории уже просмотрены", 1000);
				return;
				}

			// Запуск
			GMJ.RequestRecord ((uint)post);
			fromCategory = true;
			GetJoke_Click (null, null);
#endif
			}
		}
	}
