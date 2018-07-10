using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RoslynQuoter
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		private readonly NodeKind[] _kinds;

		public MainPage()
		{
			this.InitializeComponent();

			_kinds = new[] {
				NodeKind.CompilationUnit,
				NodeKind.Statement,
				NodeKind.Expression
			};

			comboParseAs.ItemsSource = _kinds;
			comboParseAs.SelectedIndex = 0;
		}

		private void OnCodeChanged(object sender, TextChangedEventArgs e)
		{

		}

		private void OnGenerateCode(object sender, RoutedEventArgs e)
		{
			string responseText = "";

			try
			{
				Console.WriteLine("OnGenerateCode");

				string sourceText = inputCode.Text;

				var nodeKind = _kinds[comboParseAs.SelectedIndex];
				bool openCurlyOnNewLine = checkBoxOpenParenthesis.IsChecked ?? false;
				bool closeCurlyOnNewLine = checkBoxCloseParenthesis.IsChecked ?? false;
				bool preserveOriginalWhitespace = checkBoxPreserveWhiteSpace.IsChecked ?? false;

				bool keepRedundantApiCalls = checkBoxKeepRedundant.IsChecked ?? false;

				bool avoidUsingStatic = checkBoxNoSyntaxFactory.IsChecked ?? false;


				if (string.IsNullOrEmpty(sourceText))
				{
					responseText = "Please specify the source text.";
				}
				else
				{
					Console.WriteLine("OnGenerateCode 2");

					var quoter = new Quoter
					{
						OpenParenthesisOnNewLine = openCurlyOnNewLine,
						ClosingParenthesisOnNewLine = closeCurlyOnNewLine,
						UseDefaultFormatting = !preserveOriginalWhitespace,
						RemoveRedundantModifyingCalls = !keepRedundantApiCalls,
						ShortenCodeWithUsingStatic = !avoidUsingStatic
					};

					responseText = quoter.QuoteText(sourceText, nodeKind);
				}

			}
			catch (Exception ex)
			{
				responseText = "Congratulations! You've found a bug in Quoter! Please open an issue " +
					"at https://github.com/KirillOsenkov/RoslynQuoter/issues/new and " +
					"paste the code you've typed above and this stack:";
				responseText += ex.ToString();
			}

			result.Text = responseText;
		}

		private async void OnForkMe(object sender, TappedRoutedEventArgs e)
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/nventive/Uno.RoslynQuoter"));
		}
	}
}
