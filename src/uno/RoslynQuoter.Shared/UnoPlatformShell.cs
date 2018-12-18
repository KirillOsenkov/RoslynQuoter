using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace RoslynQuoter.Shared
{
	public partial class UnoPlatformShell : Control
	{
		private Button _openAboutButton;
		private Button _closeAboutButton;
		private Button _softDismissAboutButton;
		private Button _visitUnoWebsiteButton;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_openAboutButton = GetTemplateChild("openAboutButton") as Button;
			_openAboutButton.Click += showAbout;

			_closeAboutButton = GetTemplateChild("closeAboutButton") as Button;
			_closeAboutButton.Click += hideAbout;

			_softDismissAboutButton = GetTemplateChild("softDismissAboutButton") as Button;
			_softDismissAboutButton.Click += hideAbout;

			_visitUnoWebsiteButton = GetTemplateChild("visitUnoWebsiteButton") as Button;
			_visitUnoWebsiteButton.Click += openUnoWebsite;
		}

		// showAbout
		private void showAbout(object sender, RoutedEventArgs e)
		{
			AboutVisibility = Visibility.Visible;
		}

		// hideAbout
		private void hideAbout(object sender, RoutedEventArgs e)
		{
			AboutVisibility = Visibility.Collapsed;
		}

		// openUnoWebsite
		private async void openUnoWebsite(object sender, RoutedEventArgs e)
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri("https://platform.uno"));
		}

		// App Name
		public string AppName
		{
			get { return (string)GetValue(AppNameProperty); }
			set { SetValue(AppNameProperty, value); }
		}

		public static readonly DependencyProperty AppNameProperty =
			DependencyProperty.Register("AppName", typeof(string), typeof(UnoPlatformShell), new PropertyMetadata(null));

		// App Author
		public string AppAuthor
		{
			get { return (string)GetValue(AppAuthorProperty); }
			set { SetValue(AppAuthorProperty, value); }
		}

		public static readonly DependencyProperty AppAuthorProperty =
			DependencyProperty.Register("AppAuthor", typeof(string), typeof(UnoPlatformShell), new PropertyMetadata(null));

		// Link to Original App
		public string LinkToOriginalApp
		{
			get { return (string)GetValue(LinkToOriginalAppProperty); }
			set { SetValue(LinkToOriginalAppProperty, value); }
		}

		public static readonly DependencyProperty LinkToOriginalAppProperty =
			DependencyProperty.Register("LinkToOriginalApp", typeof(string), typeof(UnoPlatformShell), new PropertyMetadata(null));

		// Link to Uno Platforml App
		public string LinkToUnoPlatformlApp
		{
			get { return (string)GetValue(LinkToUnoPlatformlAppProperty); }
			set { SetValue(LinkToUnoPlatformlAppProperty, value); }
		}

		public static readonly DependencyProperty LinkToUnoPlatformlAppProperty =
			DependencyProperty.Register("LinkToUnoPlatformlApp", typeof(string), typeof(UnoPlatformShell), new PropertyMetadata(null));

		// VersionNumber
		public string VersionNumber
		{
			get { return (string)GetValue(VersionNumberProperty); }
			set { SetValue(VersionNumberProperty, value); }
		}

		public static readonly DependencyProperty VersionNumberProperty =
			DependencyProperty.Register("VersionNumber", typeof(string), typeof(UnoPlatformShell), new PropertyMetadata(null));

		// About Content
		public object AboutContent
		{
			get { return (object)GetValue(AboutContentProperty); }
			set { SetValue(AboutContentProperty, value); }
		}

		public static readonly DependencyProperty AboutContentProperty =
			DependencyProperty.Register("AboutContent", typeof(object), typeof(UnoPlatformShell), new PropertyMetadata(null));

		// App Content
		public object AppContent
		{
			get { return (object)GetValue(AppContentProperty); }
			set { SetValue(AppContentProperty, value); }
		}

		public static readonly DependencyProperty AppContentProperty =
			DependencyProperty.Register("AppContent", typeof(object), typeof(UnoPlatformShell), new PropertyMetadata(null));


		// AboutIsVisible
		public Visibility AboutVisibility
		{
			get { return (Visibility)GetValue(AboutVisibilityProperty); }
			set { SetValue(AboutVisibilityProperty, value); }
		}

		public static readonly DependencyProperty AboutVisibilityProperty =
			DependencyProperty.Register("AboutVisibility", typeof(Visibility), typeof(UnoPlatformShell), new PropertyMetadata(Visibility.Collapsed));
	}
}
