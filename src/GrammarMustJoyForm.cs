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

		private char[] groupSplitter = new char[] { '\x1' };

		private ContextMenu textContextMenu;
		private int textContextSender;

		private const string fontFamily = "Calibri";
		private const int translucencyAmount = 15;

		// Контекстные меню категорий
		private ContextMenu topCategories;
		private ContextMenu genCategories;

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
		private Padding LogItemMargin
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

			this.Text = ProgramDescription.AssemblyVisibleName;
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
			ni.Icon = Properties.JokesArray.TrayIcon;
			ni.Text = ProgramDescription.AssemblyVisibleName;
			ni.Visible = true;

			ni.ContextMenu = new ContextMenu ();
			ni.ContextMenu.MenuItems.Add (new MenuItem (GMJ.GMJStatsMenuItem, BHelp_ItemClicked));
			ni.ContextMenu.MenuItems.Add (new MenuItem ("Настройки", BHelp_ItemClicked));
			ni.ContextMenu.MenuItems.Add (new MenuItem (
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout), BHelp_ItemClicked));
			ni.ContextMenu.MenuItems.Add (new MenuItem (
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit), BHelp_ItemClicked));

			ni.MouseDown += ShowHideFullText;

			// Цензурирование
			if (RDGenerics.StartedFromMSStore)
				{
				if (!GMJ.EnableCensorship)
					GMJ.EnableCensorship = true;
				}

			// Контекстное меню журнала
			textContextMenu = new ContextMenu ();
			textContextMenu.MenuItems.Add (new MenuItem ("Копировать текст", TextContext_ItemClicked));
			textContextMenu.MenuItems.Add (new MenuItem ("Сохранить картинку", TextContext_ItemClicked));

			// Окно сохранения картинок
			SFDialog.Title = "Укажите расположение для сохраняемой картинки";
			SFDialog.Filter = "Portable network graphics (*.png)|*.png";

			// Загрузка категорий верхнего уровня
#if TGB
			LastCategoryButton.Enabled = /*AllCategoriesButton.Enabled =*/ false;
#else
			string[] topCat = GMJ.GetTopCategories ();
			topCategories = new ContextMenu ();
			genCategories = new ContextMenu ();

			for (int i = 0; i < topCat.Length; i++)
				topCategories.MenuItems.Add (new MenuItem (topCat[i].ToUpper (), SelectTopCategory));

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
			int idx = (int)NotificationsSupport.LogColor;
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
				bw.ReportProgress ((int)(HardWorkExecutor.ProgressBarSize * (i + 1) / group),
					"Запрошено " + (i + 1).ToString () + limit);
				}

			e.Result = res;
			}

		private void GetJoke_Click (object sender, EventArgs e)
			{
			/*#if !TGB
						string[] tc = GMJ.GetTopCategories ();
						int tcn = RDGenerics.RND.Next (tc.Length);
						string ret = tc[tcn] + RDLocale.RN;

						string[] ct = GMJ.GetCategories ((uint)tcn);
						int ctn = RDGenerics.RND.Next (ct.Length);
						ret += ct[ctn] + RDLocale.RN;

						int rc = GMJ.GetRandomFromCategory ((uint)ctn);
						ret += rc.ToString ();
						AddTextToLayout (ret);
						return;
			#endif*/

			/* !!! временно !!! */
			/*#if !TGB
			RDInterface.RunWork (GetJokeExecutor, null, "Запрос случайной записи...",
				RDRunWorkFlags.CaptionInTheMiddle);
			
			System.Collections.Generic.List<string> tc =
				new System.Collections.Generic.List<string> (GMJ.GetTopCategories ());

			string req = RDInterface.MessageBox ("Введите категорию", true, 20);

			int ridx = tc.IndexOf (req.Substring (0, 1));
			if (ridx < 0)
				{
				RDInterface.MessageBox (RDMessageTypes.Error_Center, "Категория не найдена", 1000);
				return;
				}

			System.Collections.Generic.List<string> ct =
				new System.Collections.Generic.List<string> (GMJ.GetCategories ((uint)ridx));

			int cat = ct.IndexOf (req);
			if (cat < 0)
				{
				RDInterface.MessageBox (RDMessageTypes.Error_Center, "Категория не найдена", 1000);
				return;
				}

			for (int i = 0; (i < GMJ.genCatIndexes[cat].Count) && (i < NotificationsSupport.MasterLogMaxItems); i++)
				{
				GMJ.RequestRecord (GMJ.genCatIndexes[cat][i]);
				RDInterface.RunWork (GetJokeExecutor, null, "Запрос записи...",
					RDRunWorkFlags.CaptionInTheMiddle);

				string ret = RDInterface.WorkResultAsString.Replace (groupSplitter[0].ToString (), "");
				AddTextToLayout (ret);

				Thread.Sleep (2000);
				}

			return;
			#endif*/

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
			textContextMenu.MenuItems[1].Enabled = !l.Text.Contains (GMJ.SourceNoReturnPattern) &&
				!l.Text.Contains (GMJ.NoConnectionPattern) && (GMJPicture.AlignString (l.Text) != null);

			textContextMenu.Show (l, e.Location);
			}

		// Выбор варианта в меню элемента в журнале
		private void TextContext_ItemClicked (object sender, EventArgs e)
			{
			int idx = textContextMenu.MenuItems.IndexOf ((MenuItem)sender);
			if (textContextSender < 0)
				return;
			Label l = (Label)MainLayout.Controls[textContextSender];

			switch (idx)
				{
				// Копировать в буфер
				case 0:
					string notItem = l.Text;
					string notLink = "";

					int left, right;
					if (((left = notItem.IndexOf (GMJ.NumberStringBeginning)) >= 0) &&
						((right = notItem.IndexOf (GMJ.NumberStringEnding, left)) >= 0))
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
					if (text.EndsWith ("]"))
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
			int idx = ni.ContextMenu.MenuItems.IndexOf ((MenuItem)sender);

			// Вызов
			switch (idx)
				{
				case 0:
					RDInterface.MessageBox (RDMessageTypes.Information_Left, GMJ.GMJStats);
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
			if (RDInterface.MessageBox (RDMessageTypes.Question_Center, GMJ.SuggestionMessage,
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)) != RDMessageButtons.ButtonOne)
				return;

			AboutForm.AskDeveloper ();
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
			// Выбор варианта действий
			RDMessageButtons mode = RDInterface.MessageBox (RDMessageTypes.Warning_Center,
				"Выберите требуемое действие",
				"Пост",
				"Исключение",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel));

			if (mode == RDMessageButtons.ButtonThree)
				return;

			// Добавление исключения
			string post;
			string path = RDGenerics.AppStartupPath + "..\\" +
				ProgramDescription.AssemblyMainName + "\\" + "GMJ.rnw";

			if (mode == RDMessageButtons.ButtonTwo)
				{
				mode = RDInterface.MessageBox (RDMessageTypes.Warning_Center,
					"Выберите тип исключения",
					"Номер TG",
					"Номер VK");

				post = RDInterface.MessageBox ("Требуется номер поста", true, 50);
				if (string.IsNullOrWhiteSpace (post) || (post.Length < 4))
					{
					RDInterface.MessageBox (RDMessageTypes.Error_Center, "Номер не указан", 1500);
					return;
					}

				System.IO.File.AppendAllText (path,
					RDLocale.RN + (mode == RDMessageButtons.ButtonOne ? ">t>" : ">v>") +
					post + RDLocale.RNRN,
					RDGenerics.GetEncoding (RDEncodings.Unicode16));
				RDInterface.MessageBox (RDMessageTypes.Success_Center, "Успешно добавлено", 1500);

				return;
				}

			// Добавление поста
			RDInterface.MessageBox (RDMessageTypes.Warning_Center, "Требуется копия поста в буфере обмена");
			post = RDGenerics.GetFromClipboard ();
			if (string.IsNullOrWhiteSpace (post))
				{
				RDInterface.MessageBox (RDMessageTypes.Error_Center,
					"Пост не найден в буфере обмена", 1500);
				return;
				}

			// Запрос ссылки с индексом
			RDInterface.MessageBox (RDMessageTypes.Warning_Center, "Требуется копия ссылки на пост");
			string number = RDGenerics.GetFromClipboard ();
			if (string.IsNullOrWhiteSpace (number) || (number.Length < 4))
				{
				RDInterface.MessageBox (RDMessageTypes.Error_Center,
					"Ссылка на пост не найдена в буфере обмена", 1500);
				return;
				}

			// Обработка
			number = number.Substring (number.Length - 4, 4);
			
			int idx = post.IndexOf ("⁂");
			if (idx >= 0)
				post = post.Substring (0, idx);

			string lastVKNumber = "";
			if (post.StartsWith ("- №"))
				{
				lastVKNumber = post.Substring (3, 4);

				idx = post.IndexOf ("\n");
				if (idx >= 0)
					post = post.Substring (idx);
				}
			while (post.EndsWith ("\r") || post.EndsWith ("\n"))
				post = post.Substring (0, post.Length - 1);
			while (post.StartsWith ("\r") || post.StartsWith ("\n"))
				post = post.Substring (1);

			// Добавление
			System.IO.File.AppendAllText (path,
				RDLocale.RN + ">>>" + number + RDLocale.RNRN + post + RDLocale.RNRN + "<<<" + RDLocale.RNRN,
				RDGenerics.GetEncoding (RDEncodings.Unicode16));

			if (!string.IsNullOrWhiteSpace (lastVKNumber))
				System.IO.File.AppendAllText (path,
					RDLocale.RN + ">#>" + lastVKNumber + RDLocale.RNRN,
					RDGenerics.GetEncoding (RDEncodings.Unicode16));

			RDInterface.MessageBox (RDMessageTypes.Success_Center, "Успешно добавлено", 1500);

#else
			topCategories.Show (LastCategoryButton, Point.Empty);
#endif
			}

		private void SelectTopCategory (object sender, EventArgs e)
			{
			// Сброс списков
			genCategories.MenuItems.Clear ();

			// Запрос доступных категорий
			int idx = topCategories.MenuItems.IndexOf ((MenuItem)sender);
			if (idx < 0)
				return;

			// Запрос
			lastTopCategoryIndex = idx;
			RDInterface.RunWork (CategoriesRequest, null, "Загрузка категорий...", RDRunWorkFlags.CaptionInTheMiddle);

			// Контроль
			if (categoriesReqResult.Length < 1)
				{
				RDInterface.MessageBox (RDMessageTypes.Information_Center,
					"Все записи из этой категории уже просмотрены", 1000);
				return;
				}

			// Загрузка
			if (categoriesReqResult.Length <= categoriesPerMenu)
				{
				for (int i = 0; i < categoriesReqResult.Length; i++)
					genCategories.MenuItems.Add (new MenuItem (categoriesReqResult[i], SelectCategory));
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

					genCategories.MenuItems.Add (new MenuItem (left, SelectCategoryGroup));
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
			MenuItem b = (MenuItem)sender;
			int offset = genCategories.MenuItems.IndexOf (b) * categoriesPerMenu;
			if (offset < 0)
				return;

			genCategories.MenuItems.Clear ();
			for (int i = offset; (i < offset + categoriesPerMenu) && (i < categoriesReqResult.Length); i++)
				genCategories.MenuItems.Add (new MenuItem (categoriesReqResult[i], SelectCategory));

			// Запуск третьего меню
			genCategories.Tag = offset;

			if (genCategories.MenuItems.Count < 2)
				SelectCategory (genCategories.MenuItems[0], null);	// Напрямую, т.к. остался один вариант
			else
				genCategories.Show (LastCategoryButton, Point.Empty);
			}

		private void SelectCategory (object sender, EventArgs e)
			{
			// Контроль
			MenuItem b = (MenuItem)sender;
			int idx = genCategories.MenuItems.IndexOf (b) + (int)genCategories.Tag;
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
				RDInterface.MessageBox (RDMessageTypes.Information_Center,
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
