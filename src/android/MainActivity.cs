using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Maui;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает загрузчик приложения
	/// </summary>
	[Activity (Label = "JokesArray",
		Icon = "@drawable/icon",
		Theme = "@style/SplashTheme",
		MainLauncher = true,
		Name = "com.RD_AAOW.MainActivity",
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity: MauiAppCompatActivity
		{
		/// <summary>
		/// Принудительная установка масштаба шрифта
		/// </summary>
		/// <param name="base">Существующий набор параметров</param>
		protected override void AttachBaseContext (Context @base)
			{
			if (baseContextOverriden)
				{
				base.AttachBaseContext (@base);
				return;
				}

			Android.Content.Res.Configuration overrideConfiguration = new Android.Content.Res.Configuration ();
			overrideConfiguration = @base.Resources.Configuration;
			overrideConfiguration.FontScale = 0.9f;

			Context context = @base.CreateConfigurationContext (overrideConfiguration);
			baseContextOverriden = true;

			base.AttachBaseContext (context);
			}
		private bool baseContextOverriden = false;

		/// <summary>
		/// Обработчик события создания экземпляра
		/// </summary>
		protected override void OnCreate (Bundle savedInstanceState)
			{
			// Отмена темы для splash screen
			base.SetTheme (Microsoft.Maui.Controls.Resource.Style.MainTheme);

			// Запуск
			if (RDInterface.KeepScreenOn)
				this.Window.AddFlags (WindowManagerFlags.KeepScreenOn);

			// Инициализация и запуск
			base.OnCreate (savedInstanceState);

			// Необходимо для правильного расчёта размера страницы без учёта навигационной панели
			this.Window.DecorView.SystemUiFlags = SystemUiFlags.ImmersiveSticky;
			//this.Window.InsetsController.SystemBarsBehavior = 0x00000002;
			}
		}
	}
