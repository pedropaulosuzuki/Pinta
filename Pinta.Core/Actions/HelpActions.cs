// 
// HelpActions.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace Pinta.Core;

public sealed class HelpActions
{
	public Command Contents { get; }
	public Command Website { get; }
	public Command Bugs { get; }
	public Command Translate { get; }

	private readonly SystemManager system;
	private readonly AppActions app;
	public HelpActions (
		SystemManager system,
		AppActions app)
	{
		Contents = new Command (
			"contents",
			Translations.GetString ("Contents"),
			null,
			Resources.StandardIcons.HelpBrowser,
			shortcuts: ["F1"]);

		Website = new Command (
			"website",
			Translations.GetString ("Pinta Website"),
			null,
			Resources.Icons.HelpWebsite);

		Bugs = new Command (
			"bugs",
			Translations.GetString ("File a Bug"),
			null,
			Resources.Icons.HelpBug);

		Translate = new Command (
			"translate",
			Translations.GetString ("Translate This Application"),
			null,
			Resources.Icons.HelpTranslate);

		this.system = system;
		this.app = app;
	}
	public void RegisterActions (Gtk.Application application, Gio.Menu menu)
	{
		menu.AppendItem (Contents.CreateMenuItem ());
		menu.AppendItem (Website.CreateMenuItem ());
		menu.AppendItem (Bugs.CreateMenuItem ());
		menu.AppendItem (Translate.CreateMenuItem ());

		application.AddCommands ([
			Contents,
			Website,
			Bugs,
			Translate]);

		// This is part of the application menu on macOS.
		if (system.OperatingSystem != OS.Mac) {

			var about_section = Gio.Menu.New ();
			menu.AppendSection (null, about_section);

			Command about = app.About;
			application.AddCommand (about);
			about_section.AppendItem (about.CreateMenuItem ());
		}
	}

	public void RegisterHandlers ()
	{
		Contents.Activated += DisplayHelp;
		Website.Activated += Website_Activated;
		Bugs.Activated += Bugs_Activated;
		Translate.Activated += Translate_Activated;
	}

	private async void Bugs_Activated (object sender, EventArgs e)
	{
		await system.LaunchUri ("https://github.com/PintaProject/Pinta/issues");
	}

	private async void DisplayHelp (object sender, EventArgs e)
	{
		await system.LaunchUri ("https://pinta-project.com/user-guide");
	}

	private async void Translate_Activated (object sender, EventArgs e)
	{
		await system.LaunchUri ("https://hosted.weblate.org/engage/pinta/");
	}

	private async void Website_Activated (object sender, EventArgs e)
	{
		await system.LaunchUri ("https://www.pinta-project.com");
	}
}
