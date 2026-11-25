using Microsoft.Maui.Controls;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает страницу журнала программы
	/// </summary>
	/*[XamlCompilation (Xaml CompilationOptions.Compile)]*/
	public partial class LogPage: ContentPage
		{
		/// <summary>
		/// Конструктор. Запускает страницу
		/// </summary>
		public LogPage ()
			{
			InitializeComponent ();
			}

		// Исправление дефекта интерфейса MAUI, позволяющего обрушить приложение
		// нажатием системной кнопки Назад на главной странице. Применимо, соответственно,
		// только к главной странице
		protected override bool OnBackButtonPressed ()
			{
			return true;
			}
		}
	}
