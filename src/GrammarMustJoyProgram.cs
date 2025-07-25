﻿using System;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает точку входа приложения
	/// </summary>
	public static class UniNotifierProgram
		{
		/// <summary>
		/// Главная точка входа для приложения
		/// </summary>
		[STAThread]
		public static void Main (string[] args)
			{
			// Инициализация
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			RDLocale.InitEncodings ();

			// Язык интерфейса и контроль XPUN
			if (!RDLocale.IsXPUNClassAcceptable)
				return;

			// Проверка запуска единственной копии
			if (!RDGenerics.IsAppInstanceUnique (true))
				return;

			// Контроль прав
			if (!RDGenerics.AppHasAccessRights (true, false))
				return;

			// !!! Миграция из GMJ
			string gmjExists = RDGenerics.GetDPArrayRegistryValue ("GrammarMustJoy");
			string jaVersion = RDGenerics.GetSettings (RDAboutForm.LastShownVersionKey, "");
			if (!string.IsNullOrWhiteSpace (gmjExists) && string.IsNullOrWhiteSpace (jaVersion))
				NotificationsSupport.MigrateSettingsFromGMJ ();

			// Отображение справки и запроса на принятие Политики
			if (!RDInterface.AcceptEULA ())
				return;
			RDInterface.ShowAbout (true);

			// Запуск
			Application.Run (new GrammarMustJoyForm ((args.Length > 0) && (args[0] == "-h")));
			}
		}
	}
