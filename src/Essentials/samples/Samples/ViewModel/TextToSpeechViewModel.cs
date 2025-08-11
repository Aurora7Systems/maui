using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Generic;

namespace Samples.ViewModel
{
	public class TextToSpeechViewModel : BaseViewModel
	{
		CancellationTokenSource cts;

		string text;
		bool advancedOptions;
		float volume;
		float pitch;
		string locale = "Default";
		Locale selectedLocale;

		public TextToSpeechViewModel()
		{
			SpeakCommand = new Command<bool>(OnSpeak);
			CancelCommand = new Command(OnCancel);
			PickLocaleCommand = new Command(async () => await OnPickLocale());

			Text = "Hi Greg! This is a test of the text to speech functionality in .NET MAUI. " +
				"Please select a locale and try speaking this text. " +
				"Feel free to modify the text as you like.";

			AdvancedOptions = false;
			Volume = 1.0f;
			Pitch = 1.0f;
		}

		public override void OnDisappearing()
		{
			OnCancel();

			base.OnDisappearing();
		}

		void OnSpeak(bool multiple)
		{
			if (IsBusy)
				return;

			IsBusy = true;

			cts = new CancellationTokenSource();

			SpeechOptions options = null;
			if (AdvancedOptions)
			{
				options = new SpeechOptions
				{
					Volume = Volume,
					Pitch = Pitch,
					Locale = selectedLocale
				};
			}
			Debug.WriteLine("selectedLocale: " + selectedLocale?.Name);
			Task speaks = null;
			if (multiple)
			{
				speaks = Task.WhenAll(
					TextToSpeech.SpeakAsync(Text + " 1 ", options, cancelToken: cts.Token),
					TextToSpeech.SpeakAsync(Text + " 2 ", options, cancelToken: cts.Token),
					TextToSpeech.SpeakAsync(Text + " 3 ", options, cancelToken: cts.Token));
			}
			else
			{
				speaks = TextToSpeech.SpeakAsync(Text, options, cts.Token);
			}

			// use ContinueWith so we don't have to catch the cancelled exceptions
			speaks.ContinueWith(t => IsBusy = false);
		}

		void OnCancel()
		{
			if (!IsBusy && (cts?.IsCancellationRequested ?? true))
				return;

			cts.Cancel();

			IsBusy = false;
		}
		
		public class LocalePickerPage : ContentPage
		{
			public LocalePickerPage(string[] options, Action<string> onPicked)
			{
				Title = "Pick";
				var listView = new ListView
				{
					ItemsSource = options
				};
				listView.ItemSelected += (s, e) =>
				{
					if (e.SelectedItem != null)
					{
						onPicked?.Invoke(e.SelectedItem.ToString());
						Navigation.PopModalAsync();
					}
				};
				Content = new StackLayout
				{
					Children = { listView }
				};
			}
		}



		async Task OnPickLocale()
		{
			var allLocales = await TextToSpeech.GetLocalesAsync();

			Debug.WriteLine($"Available locales: {string.Join(Environment.NewLine, allLocales.Select(l => $"{l.Id} ({l.Name}) [{l.Language}, {l.Country}])"))}");
			var locales = allLocales
				.OrderBy(i => i.Language.ToLowerInvariant())
				.ToArray();

			var ids = locales
				.OrderBy(i => i.Language.ToLowerInvariant())
				.ThenBy(i => i.Country?.ToLowerInvariant() ?? string.Empty)
				.Select(i => string.IsNullOrEmpty(i.Country) ? i.Id : $"{i.Id} : {i.Language} ({i.Country})")
				.ToArray();

			var languages = locales
				.Select(i => string.IsNullOrEmpty(i.Country) ? i.Language : $"{i.Language} ({i.Country})")
				.ToArray();

			var cultures = locales
				.OrderBy(l => l.Language.ToLowerInvariant())
				.Select(l => new CultureInfo(l.Language).DisplayName)
				.ToList();

			cultures.Sort();

			Debug.WriteLine($"Available cultures: {string.Join(", ", cultures)}");

			//var selectedCulture = "English (United States)";


			string picked = null;
			await Application.Current.Windows[0].Page.Navigation.PushModalAsync(
				new LocalePickerPage(ids, result => picked = result)
			);
			// Wait for the modal to close
			while (picked == null)
				await Task.Delay(100);

			var result = picked;
			Debug.WriteLine($"Selected locale: {result}");

			//Debug.WriteLine($"Available locales: {string.Join(", ", languages)}");

			if (!string.IsNullOrEmpty(result) && Array.IndexOf(ids, result) is int idx && idx != -1)
			{
				selectedLocale = locales[idx];
				Debug.WriteLine($"Selected locale: {selectedLocale.Language} ({selectedLocale.Country})");
				Debug.WriteLine($"Selected locale ID: {selectedLocale.Id}");
				Debug.WriteLine($"Selected locale Name: {selectedLocale.Name}");
				Locale = selectedLocale.Name;
			}
			else
			{
				selectedLocale = null;
				Locale = "Default";
			}
		}

		public ICommand CancelCommand { get; }

		public ICommand SpeakCommand { get; }

		public ICommand PickLocaleCommand { get; }

		public string Text
		{
			get => text;
			set => SetProperty(ref text, value);
		}

		public bool AdvancedOptions
		{
			get => advancedOptions;
			set => SetProperty(ref advancedOptions, value);
		}

		public float Volume
		{
			get => volume;
			set => SetProperty(ref volume, value);
		}

		public float Pitch
		{
			get => pitch;
			set => SetProperty(ref pitch, value);
		}

		public string Locale
		{
			get => locale;
			set => SetProperty(ref locale, value);
		}
	}
}
