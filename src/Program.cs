using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Chesh.Controller;
using Chesh.Model;
using Chesh.View;

namespace Chesh
{
  public class Program
  {

    static void
    Main(string[] args)
    {
      List<string> load;
      float speed;
      string style;
      if (! Args(args, out style, out load, out speed))
      {
        return;
      }
      Game game = new Game(style, null);
      Ui ui = new Ui(game);
      Control ctrl = new Control(game, ui);
      game.Attach(ctrl);
      //game.StateChanged(game.State);
      ui.SetControl(ctrl);
      ui.Play(load, speed);
    }


    // Args: Parse commandline arguments.
    //       Position and order are inflexible.

    public static bool
    Args(string[] args,
         out string style, out List<string> load, out float speed)
    {
      style = "compact";
      load = Load(args);
      speed = 0;

      // arg 1: style, compact (default) or wide
      if (args.Length >= 1)
      {
        if (args[0] == "w" || args[0] == "wide")
        {
          style = "wide";
        }
        else if (args[0] == "c" || args[0] == "compact")
        {
          style = "compact";
        }
        else
        {
          Console.WriteLine("Unknown style: " + args[0]);
          return false;
        }
      }

      // arg 2: filename, dealt with already by Load()
      if (load == null)
      {
        Console.WriteLine("Could not find file: " + args[1]);
        return false;
      }

      // arg 3: playback speed, float (default 0)
      if (args.Length >= 3)
      {
        try
        {
          speed = float.Parse(args[2], CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
          Console.WriteLine("Could not parse float: " + args[2]);
          return false;
        }
      }
      return true;
    }


    // Load: Prepare for playback loading with user-provided file.
    //       The file requires specific syntax; see documentation.

    public static List<string>
    Load(string[] args)
    {
      var moves = new List<string>();

      // filename not provided
      if (args.Length < 2)
      {
        return moves;
      }

      // file does not exist
      if (!File.Exists(args[1]))
      {
        return null;
      }

      // check readability
      try
      {
        using (var f = new FileStream(args[1], FileMode.Open, FileAccess.Read))
        {}
      }
      catch (Exception)
      {
        return null;
      }

      // pack all words into a list
      using (var reader = new StreamReader(args[1]))
      {
        string line;
        while ((line = reader.ReadLine()) != null)
        {
          if (string.IsNullOrWhiteSpace(line) || line[0] == '#')
          {
            continue;
          }
          foreach (var word in
                   line.Split(" ", StringSplitOptions.RemoveEmptyEntries))
          {
            moves.Add(word);
          }
        }
      }
      return moves;
    }


    // Save: Save the game to the predetermined file.

    public static void
    Save(List<(string,long)> history)
    {
      int count = 0;
      string last = new string(' ', 8);
      using (var writer = new StreamWriter("chesh.log"))
      {
        writer.WriteLine("# " + (new DateTime(DateTime.Now.Ticks)).Ticks);
        foreach (var note in history)
        {
          if (count % 2 == 0)
          {
            writer.Write(note.Item1);
          }
          else
          {
            writer.Write(new string(' ', 8 - last.Length));
            writer.WriteLine(note.Item1);
          }
          count++;
          last = note.Item1;
        }
        if (history.Count % 2 == 1)
        {
          writer.WriteLine();
        }
        writer.WriteLine();
      }
    }
  }
}
