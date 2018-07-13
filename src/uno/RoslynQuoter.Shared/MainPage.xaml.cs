using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

			buildVersion.Text = $"Build: {this.GetType().GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unkown"}";

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
			result.Text = GenerateCode();
		}

		private async void OnGenerateLinqPadCode(object sender, RoutedEventArgs e)
		{
			var linqpadFile = $@"<Query Kind=""Expression"">
				  <NuGetReference>Microsoft.CodeAnalysis.Compilers</NuGetReference>
				  <NuGetReference>Microsoft.CodeAnalysis.CSharp</NuGetReference>
				  <Namespace>static Microsoft.CodeAnalysis.CSharp.SyntaxFactory</Namespace>
				  <Namespace>Microsoft.CodeAnalysis.CSharp.Syntax</Namespace>
				  <Namespace>Microsoft.CodeAnalysis.CSharp</Namespace>
				  <Namespace>Microsoft.CodeAnalysis</Namespace>
				</Query>

				{GenerateCode()}
			";


#if !__WASM__
			try
			{
				var savePicker = new Windows.Storage.Pickers.FileSavePicker();

				savePicker.SuggestedStartLocation =
					Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
				savePicker.FileTypeChoices.Add("LINQPad File", new List<string>() { ".linq" });
				savePicker.SuggestedFileName = "Quoter";

				Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();

				// Prevent updates to the remote version of the file until
				// we finish making changes and call CompleteUpdatesAsync.
				Windows.Storage.CachedFileManager.DeferUpdates(file);

				// write to file
				await Windows.Storage.FileIO.WriteTextAsync(file, linqpadFile);

				// Let Windows know that we're finished changing the file so
				// the other app can update the remote version of the file.
				// Completing updates may require Windows to ask for user input.
				Windows.Storage.Provider.FileUpdateStatus status =
					await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
			}
#else
			SaveFileAs("Quoter.linq", linqpadFile);
#endif
		}

#if __WASM__
		private static void SaveFileAs(string fileName, string content)
		{
			var data = Encoding.UTF8.GetBytes(content);

			GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);
			var pinnedData = gch.AddrOfPinnedObject();

			try
			{
				Console.WriteLine("Invoking saveAs...");
				Uno.Foundation.WebAssemblyRuntime.InvokeJS($@"fileSaveAs('{fileName}', {pinnedData}, {data.Length})");
			}
			finally
			{
				gch.Free();
			}
		}
#endif


		private string GenerateCode()
		{
			string responseText = "";

			try
			{
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

			return responseText;
		}

		private async void OnForkMe(object sender, TappedRoutedEventArgs e)
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/nventive/Uno.RoslynQuoter"));
		}
	}
}
