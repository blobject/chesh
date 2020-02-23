using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Chesh.Controller;
using Chesh.Model;
using Chesh.Util;

namespace Chesh.View
{

  // Res: Enumeration of the elements that can display responses.

  public enum Res { Response, Prompt, Menu }


  // Ui: The Console-based game view.

  public class Ui
  {
    public static int HostWidth;
    public static int HostHeight;
    public static int Width;
    public static int Height;
    public Dictionary<string,Element> Es { get; set; }
    public string State;
    public string Cfg;
    private Game Game;
    private Control Control;
    private bool Running;

    public Ui(Game game)
    {
      HostWidth = Console.WindowWidth;
      HostHeight = Console.WindowHeight;
      Width = 26;
      Height = 19;
      this.Game = game;
      this.State = Helper.ToJson(game.State);
      this.Cfg = Helper.ToJson(game.Cfg);
      this.Es = new Dictionary<string,Element>();
      string style = Helper.JsonToCfgValue(this.Cfg, "style");
      if (style == "wide")
      {
        Width = 58;
        Height = 29;
      }
      this.Es["Menu"] = new Menu(style);
      this.Es["BlackResponse"] = new BlackResponseElement(style);
      this.Es["BlackPrompt"] = new BlackPromptElement(style);
      this.Es["History"] = new HistoryElement(style);
      this.Es["WhiteDead"] = new WhiteDeadElement(style);
      this.Es["Frame"] = new BoardFrameElement(style);
      this.Es["Pieces"] = new PiecesElement(style);
      this.Es["BlackDead"] = new BlackDeadElement(style);
      this.Es["WhiteResponse"] = new WhiteResponseElement(style);
      this.Es["WhitePrompt"] = new WhitePromptElement(style);
      this.Running = true;
    }


    // accessors ///////////////////////////////////////////////////////////////

    // GoodDimensions: Check the display size.

    public bool
    GoodDimensions(string style)
    {
      switch (style)
      {
        case "compact":
          if (HostWidth < 26 || HostHeight < 19)
          {
            return false;
          }
          break;
        case "wide":
          if (HostWidth < 58 || HostHeight < 29)
          {
            return false;
          }
          break;
      }
      return true;
    }


    // Turn: Get the current turn, returning the color as string.
    //       Based only on the parity of the number of entries in the history.

    public string
    Turn(bool current)
    {
      int count = 0;
      foreach (var note in
               Helper.JsonToStateListValue(this.State, "History"))
      {
        count++;
      }
      if (count % 2 == 0)
      {
        if (current)
        {
          return "White";
        }
        return "Black";
      }
      if (current)
      {
        return "Black";
      }
      return "White";
    }


    // At: Get the name of the piece at a square.

    public string
    At(char file, char rank)
    {
      // could simply access this.State.Board as well
      foreach (JsonElement piece in
               Helper.JsonToStateListValue(this.State, "Live"))
      {
        if (piece[2].GetInt32() == Helper.ToFileNum(file) &&
            piece[3].GetInt32() == Helper.ToRankNum(rank))
        {
          var sym = piece[0].GetString();
          return Helper.SymToName(sym);
        }
      }
      return null;
    }


    // mutators ////////////////////////////////////////////////////////////////

    // SetControl: Connect to the controller.

    public void
    SetControl(Control control)
    {
      this.Control = control;
    }


    // SetState: Update the local mirror of State (as json string).

    public void
    SetState(string state)
    {
      this.State = state;
    }


    // SetCfg: Update the local mirror of Cfg (as json string).

    public void
    SetCfg(string cfg)
    {
      this.Cfg = cfg;
      switch (Helper.JsonToCfgValue(cfg, "style"))
      {
        case "compact":
          Width = 26;
          Height = 19;
          break;
        case "wide":
          Width = 58;
          Height = 29;
          break;
      }
    }


    // Console helpers /////////////////////////////////////////////////////////

    // Write: Wrapper around Console.Write.

    public static void
    Write(string s)
    {
      Console.Write(s);
    }


    // Pin: Set the cursor's position.
    //      Wrapper around Console.SetCursorPosition.

    public static void
    Pin(int x, int y)
    {
      Console.SetCursorPosition(x, y);
    }


    // Erase: Erase the entire display.

    public void
    Erase()
    {
      Console.Clear();
      Ui.Pin(0, 0);
    }


    // User interaction helpers ////////////////////////////////////////////////

    // Say: Display a message somewhere, as a response to some interaction.

    public void
    Say(Res res, string message, bool current)
    {
      var responder = this.Es[this.Turn(current) + "Response"];
      if (res == Res.Prompt)
      {
        responder = this.Es[this.Turn(current) + "Prompt"];
      }
      else if (res == Res.Menu)
      {
        responder = new MenuResponseElement(null);
      }
      responder.Erase();
      responder.Draw(message);
    }


    // AskPromote: Allow the user to pick a piece to promote to.

    public char
    AskPromote(string prefix)
    {
      this.Say(Res.Response, $"{prefix} Promote to? (RNBQ) ", true);
      ConsoleKey k;
      while (true)
      {
        k = Console.ReadKey(true).Key;
        if (k == ConsoleKey.R)
        {
          return 'R';
        }
        if (k == ConsoleKey.N)
        {
          return 'N';
        }
        if (k == ConsoleKey.B)
        {
          return 'B';
        }
        if (k == ConsoleKey.Q)
        {
          return 'Q';
        }
      }
    }


    // Ask: Ask user for a yes/no confirmation.

    public bool
    Ask(Res res, string message, bool current, bool enterIsYes)
    {
      this.Say(res, message + " ", current);
      ConsoleKey k;
      while (true)
      {
        k = Console.ReadKey(true).Key;
        if (k == ConsoleKey.Y)
        {
          return true;
        }
        if (enterIsYes && k == ConsoleKey.Enter)
        {
          return true;
        }
        if (k == ConsoleKey.N)
        {
          return false;
        }
      }
    }


    // Gameplay ////////////////////////////////////////////////////////////////

    // Read: Detect or relay user input.

    public string
    Read()
    {
      string input = string.Empty;
      while (true)
      {
        ConsoleKeyInfo cki = Console.ReadKey(true);
        ConsoleKey k = cki.Key;
        char c = cki.KeyChar;
        if (k == ConsoleKey.Escape)
        {
          Option opt = this.Menu();
          if (opt == Option.Quit)
          {
            return "stopnow";
          }
          if (opt == Option.Undo)
          {
            return "undo";
          }
        }
        if (k == ConsoleKey.Enter)
        {
          break;
        }
        if (k == ConsoleKey.Backspace)
        {
          if (input != string.Empty)
          {
            input = input.Substring(0, input.Length - 1);
            Write("\b \b");
          }
          continue;
        }
        if ((cki.Modifiers & ConsoleModifiers.Control) != 0 &&
            (k == ConsoleKey.D))
        {
          this.Say(Res.Prompt, "quit", true);
          return "quit";
        }
        if ((cki.Modifiers & ConsoleModifiers.Control) != 0
            || (cki.Modifiers & ConsoleModifiers.Alt) != 0)
        {
          continue;
        }
        if (input.Length < this.Es[this.Turn(true) + "Prompt"].Width)
        {
          Write(c.ToString());
          input += c;
        }
      }
      return input.Trim().ToLower();
    }


    // Understand: Interpret the user input into a move.

    public void
    Understand(string input)
    {
      input = Regex.Replace(input, @"\s+", string.Empty).ToLower();
      if (input.Length != 4)
      {
        this.Say(Res.Response, input + " ?", true);
        this.Say(Res.Prompt, null, true);
        return;
      }

      string src = $"{input[0]}{input[1]}";
      string dst = $"{input[2]}{input[3]}";
      if (! Helper.ValidSquare(input[0], input[1]))
      {
        this.Say(Res.Response, "Unrecognised square: " + src, true);
        this.Say(Res.Prompt, null, true);
        return;
      }
      if (! Helper.ValidSquare(input[2], input[3]))
      {
        this.Say(Res.Response, "Unrecognised square: " + dst, true);
        this.Say(Res.Prompt, null, true);
        return;
      }
      string name = this.At(input[0], input[1]);
      if (name != null)
      {
        name += " ";
      }
      string move = $"{name}{src}->{dst}";
      if (! Ask(Res.Response, $"{move}? (y/n)", true, true))
      {
        this.Say(Res.Response, null, true);
        this.Say(Res.Prompt, null, true);
        return;
      }
      (int,int,int,int) srcdst = Helper.MoveToInts(input);
      this.Move(srcdst.Item1, srcdst.Item2,
                srcdst.Item3, srcdst.Item4,
                src, dst, false);
    }


    // Play: The main loop of the interactive view.

    public void
    Play(List<string> load, float speed)
    {
      string style = Helper.JsonToCfgValue(this.Cfg, "style");
      if (! this.GoodDimensions(style))
      {
        switch (style)
        {
          case "compact":
            Console.WriteLine("Compact style needs at least 26 x 19");
            break;
          case "wide":
            Console.WriteLine("Wide style needs at least 58 x 29");
            break;
        }
        return;
      }
      this.Erase();
      this.Draw();

      if (this.Playback(load, speed))
      {
        return;
      }

      string input;
      while (Running)
      {
        input = Read();
        switch (input)
        {
          case "":
            this.Say(Res.Prompt, null, true);
            continue;
          case "exit":
          case "quit":
          case "stop":
            this.Stop(Res.Response, "Save?", true);
            Pin(0, Height + 2);
            return;
          case "stopnow":
            Pin(0, Height + 2);
            return;
          case "undo":
            break;
          case "draw":
          case "tie":
            if (this.Tie())
            {
              return;
            }
            break;
          case "forfeit":
          case "giveup":
          case "resign":
            this.Say(Res.Response,
                     $"{this.Turn(true)} resigned. {this.Turn(false)} wins!",
                     false);
            this.Stop(Res.Prompt, "Save before quitting?", false);
            Pin(0, Height + 2);
            return;
          default:
            this.Understand(input);
            break;
        }
      }
    }


    // Playback: Automated play, given that the game was passed the appropriate
    //           commandline arguments.

    public bool
    Playback(List<string> load, float speed)
    {
      int count = 0;
      foreach (var note in load)
      {
        count++;
        string message = $"{note}: Move {count} is invalid!";
        if (note != "bye" && (note.Length < 5 || note.Length > 7))
        {
          this.Say(Res.Response, message, true);
          this.Say(Res.Prompt, null, true);
          return false;
        }
        string move = Helper.Denotate(note);
        // TODO: implement tie in playback
        if (move == "bye")
        {
          this.Say(Res.Response,
                   $"{this.Turn(true)} resigned. {this.Turn(false)} wins!",
                   true);
          this.Stop(Res.Prompt, "Save before quitting?", true);
          Pin(0, Height + 2);
          return true;
        }
        (int,int,int,int) srcdst = Helper.MoveToInts(move);
        var rets = this.Move(srcdst.Item1, srcdst.Item2,
                             srcdst.Item3, srcdst.Item4,
                             $"{move[0]}{move[1]}", $"{move[2]}{move[3]}",
                             true);
        foreach (var ret in rets)
        {
          if (! (new List<Ret>() {
                Ret.Capture,
                Ret.Castle,
                Ret.Promote,
                Ret.Check,
                Ret.Checkmate
             }).Contains(ret))
          {
            this.Say(Res.Response, message, true);
            this.Say(Res.Prompt, null, true);
            return false;
          }
        }
        if (rets.Contains(Ret.Promote))
        {
          char prom = Regex.Replace(note.Substring(5), @"[*&#+:p%]",
                                    string.Empty)[0];
          this.Control.Promote(rets, prom,
                               srcdst.Item1, srcdst.Item2,
                               srcdst.Item3, srcdst.Item4);
        }
        if (rets.Contains(Ret.Checkmate))
        {
          return true;
        }
        if (speed > 0)
        {
          Thread.Sleep((int) (speed * 1000));
        }
      }
      return false;
    }


    // Menu: Display and interact with the menu.

    public Option
    Menu()
    {
      var menu = this.Es["Menu"];
      this.Erase();
      menu.Draw(this.Cfg);

      while (true)
      {
        ConsoleKeyInfo cki = Console.ReadKey(true);
        ConsoleKey k = cki.Key;
        char c = cki.KeyChar;
        if (k == ConsoleKey.Escape)
        {
          this.Erase();
          this.Draw();
          break;
        }
        if (k == ConsoleKey.UpArrow)
        {
          menu.Nav(true, this.Cfg);
          continue;
        }
        if (k == ConsoleKey.DownArrow)
        {
          menu.Nav(false, this.Cfg);
          continue;
        }
        if (k == ConsoleKey.Enter)
        {
          Option opt = menu.Enter();
          if (opt == Option.Style)
          {
            string style = Helper.JsonToCfgValue(this.Cfg, "style");
            if (! this.GoodDimensions(style))
            {
              switch (style)
              {
                case "compact":
                  this.Say(Res.Menu, "Compact style needs at least 58 x 29", true);
                  break;
                case "wide":
                  this.Say(Res.Menu, "Wide style needs at least 26 x 19", true);
                  break;
              }
              menu.Draw(this.Cfg);
              continue;
            }
            switch (style)
            {
              case "compact":
                style = "wide";
                break;
              case "wide":
                style = "compact";
                break;
            }
            this.Control.SetStyleCfg(style);
            continue;
          }
          if (opt == Option.Resume)
          {
            this.Erase();
            this.Draw();
            return opt;
          }
          if (opt == Option.Undo)
          {
            this.Erase();
            if (! this.Undo())
            {
              this.Draw();
            }
            return opt;
          }
          if (opt == Option.Reset)
          {
            this.Erase();
            this.Reset();
            return opt;
          }
          if (opt == Option.Quit)
          {
            if (Ask(Res.Menu, "Save? (y/n)", true, false))
            {
              if (File.Exists("chesh.log") &&
                  Ask(Res.Menu, "chesh.log already exists. Sure? (y/n)",
                      true, false))
              {
                this.Running = false;
                this.Control.Save();
              }
            }
            this.Erase();
            this.Draw();
            return opt;
          }
          continue;
        }
      }
      return Option.None;
    }


    // Stop: Stop the game.

    public void
    Stop(Res res, string message, bool current)
    {
      if (Ask(res, message + " (y/n)", current, false))
      {
        if (File.Exists("chesh.log") &&
            Ask(res, "chesh.log already exists. Sure? (y/n)",
                current, false))
        {
          this.Running = false;
          this.Control.Save();
          this.Say(Res.Response, "Game log saved: chesh.log", current);
        }
      }
    }


    // Draw: Display all the elements (except the menu).

    public void
    Draw()
    {
      foreach (KeyValuePair<string,Element> element in this.Es)
      {
        if (! element.Key.EndsWith("Response") &&
            ! element.Key.EndsWith("Prompt") &&
            element.Key != "Menu")
        {
          element.Value.Draw(this.State);
        }
      }
      this.Say(Res.Response, null, false);
      this.Say(Res.Prompt, null, false);
      this.Say(Res.Response, null, true);
      this.Say(Res.Prompt, null, true);
    }


    // Reset: Reset the game.

    public void
    Reset()
    {
      this.Control.Reset();
    }


    // Tie: Make a "propose to draw" move.

    public bool
    Tie()
    {
      Ret ret = this.Control.Tie(this.Turn(true));
      if (ret == Ret.Tie)
      {
        this.Say(Res.Response, "Tied.", false);
        this.Stop(Res.Prompt, "Save before quitting?", false);
        Pin(0, Height + 2);
        return true;
      }
      if (ret == Ret.Tying)
      {
        this.Say(Res.Response,
                 $"{this.Turn(false)} proposed a tie", true);
        if (this.Ask(Res.Prompt, "Accept?", true, false))
        {
          return this.Tie();
        }
        this.Control.Untie();
        this.Say(Res.Response, "Tie declined", true);
        this.Say(Res.Prompt, null, true);
      }
      return false;
    }


    // Undo: Undo the last move.

    public bool
    Undo()
    {
      return this.Control.Undo();
    }


    // Move: Make a move.

    public List<Ret>
    Move(int xSrc, int ySrc, int xDst, int yDst,
         string src, string dst, bool playback)
    {
      string prefix = src + "->" + dst + ":";
      List<Ret> rets = this.Control.Move(xSrc, ySrc, xDst, yDst);
      if (rets.Contains(Ret.BadSrc))
      {
        this.Say(Res.Response, $"{prefix} No piece on " + src, true);
        this.Say(Res.Prompt, null, true);
        return rets;
      }
      if (rets.Contains(Ret.BadTurn))
      {
        this.Say(Res.Response, $"{prefix} Other player's turn", true);
        this.Say(Res.Prompt, null, true);
        return rets;
      }
      if (rets.Contains(Ret.BadDst))
      {
        this.Say(Res.Response, $"{prefix} A friendly on " + dst, true);
        this.Say(Res.Prompt, null, true);
        return rets;
      }
      if (rets.Contains(Ret.BadCastle))
      {
        this.Say(Res.Response, $"{prefix} Cannot castle", true);
        this.Say(Res.Prompt, null, true);
        return rets;
      }
      if (rets.Contains(Ret.InvalidMove))
      {
        this.Say(Res.Response, $"{prefix} Illegal move", true);
        this.Say(Res.Prompt, null, true);
        return rets;
      }
      if (rets.Contains(Ret.Checked))
      {
        this.Say(Res.Response, $"{prefix} King will be checked", true);
        this.Say(Res.Prompt, null, true);
        return rets;
      }
      if (rets.Contains(Ret.Checkmate))
      {
        this.Say(Res.Response, $"Checkmate. {this.Turn(false)} wins!", false);
        this.Stop(Res.Prompt, "Save before quitting?", false);
        Pin(0, Height + 2);
        return rets;
      }
      if (rets.Contains(Ret.Promote) && ! playback)
      {
        this.Control.Promote(rets, AskPromote(prefix),
                             xSrc, ySrc, xDst, yDst);
      }
      // Ret.Regular
      // Ret.Capture
      // Ret.Castle
      // Ret.Promote
      // Ret.EnPassant
      // Ret.Check
      this.Say(Res.Response, null, false);
      this.Say(Res.Response, null, true);
      this.Say(Res.Prompt, null, true);
      return rets;
    }
  }
}
