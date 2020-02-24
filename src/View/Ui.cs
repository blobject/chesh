using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Chesh.Controller;
using Chesh.Model;
using Chesh.Util;

namespace Chesh.View
{

  // InputHandler: Read and interpret user input in the Console.

  public class InputHandler
  {
    private UiState UiState;

    public InputHandler(UiState uiState)
    {
      this.UiState = uiState;
    }

    public ConsoleKey
    Read()
    {
      return Console.ReadKey(true).Key;
    }


    // Understand: React to user input.

    public bool
    Understand(ConsoleKey input)
    {
      switch (input)
      {
        case ConsoleKey.Escape:
          Ui.Pin(0, Ui.Height + 1);
          this.UiState.Running = false;
          break;
        case ConsoleKey.Enter:
          this.UiState.SetSrcOrMove(this.UiState.X, this.UiState.Y);
          break;
        case ConsoleKey.UpArrow:
          this.UiState.SetSelection(this.UiState.X, this.UiState.Y + 1);
          break;
        case ConsoleKey.RightArrow:
          this.UiState.SetSelection(this.UiState.X + 1, this.UiState.Y);
          break;
        case ConsoleKey.DownArrow:
          this.UiState.SetSelection(this.UiState.X, this.UiState.Y - 1);
          break;
        case ConsoleKey.LeftArrow:
          this.UiState.SetSelection(this.UiState.X - 1, this.UiState.Y);
          break;
/**
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
//*/
      }
      return true;
    }
  }


  // UiState: Handle Ui running and piece selection.

  public class UiState
  {
    private Ui Ui;
    public bool Running;
    public int X;
    public int Y;
    public int XSrc;
    public int YSrc;
    public bool Chosen;

    public UiState(Ui ui)
    {
      this.Ui = ui;
      this.Running = true;
      this.Chosen = false;
    }


    // InReach: Determine whether a square is within the selected piece's reach.

    public bool
    InReach(int x, int y)
    {
      foreach (JsonElement swap in
               Helper.JsonToStateList(this.Ui.State, "Reach"))
      {
        if (x == swap[0].GetInt32() && y == swap[1].GetInt32())
        {
          return true;
        }
      }
      return false;
    }


    // SetSelection: Select the piece, if any, on a square.

    public void
    SetSelection(int x, int y)
    {
      if (x < 1 || x > 8 || y < 1 || y > 8)
      {
        return;
      }
      this.X = x;
      this.Y = y;
      if (! this.Chosen)
      {
        this.Ui.Control.Select(x, y);
      }
      this.Ui.PinSquare(x, y);
    }


    // SetSrcOrMove: Set the piece about to move or make the move.

    public void
    SetSrcOrMove(int x, int y)
    {
      if (! this.Chosen)
      {
        if (this.Ui.At(x, y) != null)
        {
          // tried to choose enemy
          if (Helper.JsonIsNull(this.Ui.State, "Selection"))
          {
            this.Ui.Say("It is " + this.Ui.Turn(true) + "'s turn.");
            return;
          }
          // tried to choose immobile piece
          if (Helper.JsonToStateList(this.Ui.State, "Reach").Count() == 0)
          {
            this.Ui.Say(this.Ui.NameAt(x, y) + " is immobile.");
            return;
          }
          // chose well
          this.XSrc = x;
          this.YSrc = y;
          this.Chosen = true;
          this.Ui.Es["Reach"].Draw(this.Ui, null);
          this.Ui.PinSquare(x, y);
        }
        return;
      }
      this.Chosen = false;
      if (this.InReach(x, y))
      {
        // moved well
        var rets = this.Ui.Control.Move(this.XSrc, this.YSrc, x, y);
        this.SetSelection(x, y);
        this.MoveSay(rets, this.XSrc, this.YSrc, x, y);
        this.XSrc = 0;
        this.YSrc = 0;
        return;
      }
      // moved to empty square, so cancel chosen
      if (this.Ui.At(x, y) == null)
      {
        this.XSrc = 0;
        this.YSrc = 0;
        this.SetSelection(x, y);
        return;
      }
      // moved to same square, so cancel chosen
      if (x == this.XSrc && y == this.YSrc)
      {
        this.XSrc = 0;
        this.YSrc = 0;
        this.SetSelection(x, y);
      }
      // chose another piece, so recurse
      else
      {
        this.SetSelection(x, y);
        this.SetSrcOrMove(x, y);
      }
    }


    // MoveSay: Respond to a move.

    public void
    MoveSay(List<Ret> rets, int xSrc, int ySrc, int xDst, int yDst)
    {
      string prefix = this.Ui.Turn(false) + " " +
        this.Ui.NameAt(xDst, yDst) + " " +
        Helper.IntsToSquare(xSrc, ySrc) + " -> " +
        Helper.IntsToSquare(xDst, yDst);

      if (rets.Count == 0)
      {
        this.Ui.Say(prefix + ".");
        return;
      }

      if (rets.Contains(Ret.Checked))
      {
        this.Ui.Say("Cannot move there, King is checked.");
        return;
      }

      JsonElement died = Helper.LastDead(this.Ui.State);

      if (rets.Contains(Ret.Capture))
      {
        string message = prefix + " capturing " +
          Helper.SymToName(died[0].GetString());
        if (rets.Contains(Ret.Check))
        {
          this.Ui.Say(message + " and CHECK!");
          return;
        }
        if (rets.Contains(Ret.Checkmate))
        {
          this.Ui.Say(message + " and CHECKMATE! " + this.Ui.Turn(false) +
                      " wins!");
          //this.Stop("Save before quitting?");
          //Pin(0, Height + 2);
          return;
        }
        this.Ui.Say(message + ".");
        return;
      }

      if (rets.Contains(Ret.Castle))
      {
        string message = this.Ui.Turn(false) + " castling ";
        if (xDst < 0)
        {
          message += "queenside";
        }
        else
        {
          message += "kingside";
        }
        if (rets.Contains(Ret.Check))
        {
          this.Ui.Say(message + " and CHECK!");
          return;
        }
        if (rets.Contains(Ret.Checkmate))
        {
          this.Ui.Say(message + " and CHECKMATE! " + this.Ui.Turn(false) +
                      " wins!");
          //this.Stop("Save before quitting?");
          //Pin(0, Height + 2);
          return;
        }
        this.Ui.Say(message + ".");
        return;
      }

      if (rets.Contains(Ret.Check))
      {
        this.Ui.Say(prefix + ". CHECK!");
        return;
      }

      if (rets.Contains(Ret.Checkmate))
      {
        this.Ui.Say(prefix + ". CHECKMATE! " + this.Ui.Turn(false) + " wins!");
        //this.Stop("Save before quitting?");
        //Pin(0, Height + 2);
        return;
      }
/**
      if (rets.Contains(Ret.InvalidMove))
      {
        this.Say($"(Error) {prefix} Illegal move");
        return rets;
      }
//*/
    }
  }

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
    public Control Control;
    public UiState UiState;
    private InputHandler InputHandler;

    public Ui(Game game)
    {
      this.Game = game;
      this.State = Helper.ToJson(game.State);
      this.Cfg = Helper.ToJson(game.Cfg);
      string style = Helper.JsonToValue(this.Cfg, "style");
      Width = 26;
      Height = 19;
      if (style == "wide")
      {
        Width = 64;
        Height = 25;
      }
      HostWidth = Console.WindowWidth;
      HostHeight = Console.WindowHeight;
      this.Es = new Dictionary<string,Element>();
      this.Es["Menu"] = new Menu(style);
      this.Es["WhiteHistory"] = new WhiteHistoryElement(style);
      this.Es["BlackHistory"] = new BlackHistoryElement(style);
      this.Es["WhiteDead"] = new WhiteDeadElement(style);
      this.Es["BlackDead"] = new BlackDeadElement(style);
      this.Es["Frame"] = new BoardFrameElement(style);
      this.Es["Pieces"] = new PiecesElement(style);
      this.Es["Reach"] = new ReachElement(style);
      this.Es["Response"] = new ResponseElement(style);
      this.UiState = new UiState(this);
      this.InputHandler = new InputHandler(this.UiState);
    }


    // Mutators ////////////////////////////////////////////////////////////////

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
      switch (Helper.JsonToValue(cfg, "style"))
      {
        case "compact":
          Width = 26;
          Height = 19;
          break;
        case "wide":
          Width = 64;
          Height = 25;
          break;
      }
    }


    // Accessors ///////////////////////////////////////////////////////////////

    // At: Get the sym of the piece at a square.

    public string
    At(int file, int rank)
    {
      // could simply access State.Board as well
      foreach (JsonElement piece in
               Helper.JsonToStateList(this.State, "Live"))
      {
        if (piece[2].GetInt32() == file && piece[3].GetInt32() == rank)
        {
          return piece[0].GetString();
        }
      }
      return null;
    }


    // NameAt: Get the name of the piece at a square.

    public string
    NameAt(int file, int rank)
    {
      string sym = this.At(file, rank);
      if (sym == null)
      {
        return null;
      }
      return Helper.SymToName(sym);
    }


    // GoodDimensions: Check the display size.

    public bool
    GoodDimensions()
    {
      bool good = true;
      string style = Helper.JsonToValue(this.Cfg, "style");
      if (style == "compact" && (HostWidth < 26 || HostHeight < 19))
      {
        good = false;
      }
      else if (style == "wide" && (HostWidth < 64 || HostHeight < 25))
      {
        good = false;
      }
      if (! good)
      {
        Write(style[0].ToString().ToUpper() + style.Substring(1) +
              $" style needs at least {Width} x {Height}");
      }
      return good;
    }


    // Turn: Get the current turn, returning the color as string.
    //       Based only on the parity of the number of entries in the history.

    public string
    Turn(bool current)
    {
      int count = 0;
      foreach (var note in Helper.JsonToStateList(this.State, "History"))
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


    // Drawing helpers /////////////////////////////////////////////////////////

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


    // PinSquare: Set cursor position to the square on the board.

    public void
    PinSquare(int x, int y)
    {
      var pieces = this.Es["Pieces"];
      Pin(pieces.X + 4 * x - 5, pieces.Y + 17 - 2 * y);
    }


    // Erase: Erase the entire display.

    public void
    Erase()
    {
      Pin(0, 0);
      for (int row = 0; row < HostHeight; row++)
      {
        Pin(0, row);
        Write(new string(' ', HostWidth));
      }
      Pin(0, 0);
    }


    // Draw: Display all the elements (except the menu).

    public void
    Draw()
    {
      foreach (var element in this.Es)
      {
        if (! (element.Key == "Response" || element.Key == "Menu"))
        {
          element.Value.Draw(this, null);
        }
      }
      //this.Say(null);
    }


    // Interaction helpers /////////////////////////////////////////////////////

    // Say: Display a response to some interaction.

    public void
    Say(string message)
    {
      var responder = this.Es["Response"];
      responder.Erase();
      responder.Draw(null, message);
      this.PinSquare(this.UiState.X, this.UiState.Y);
    }


    // Ask: Ask user for a yes/no confirmation.

    public bool
    Ask(string message, bool enterIsYes)
    {
      this.Say(message + " ");
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

    // Play: The main loop of the interactive view.

    public void
    Play(List<string> load, float speed)
    {
      if (! this.GoodDimensions())
      {
        return;
      }

      this.Erase();
      this.UiState.SetSelection(5, 1); // the white king

      if (this.Playback(load, speed))
      {
        return;
      }

      while (this.UiState.Running)
      {
        this.InputHandler.Understand(this.InputHandler.Read());
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
          this.Say(message);
          return false;
        }
        string move = Helper.Denotate(note);
        // TODO: implement tie in playback
        if (move == "bye")
        {
          this.Say($"{this.Turn(true)} resigned. {this.Turn(false)} wins!");
          this.Stop("Save before quitting?");
          Pin(0, Height + 2);
          return true;
        }
        Swap srcdst = Helper.MoveToSwap(move);
        var rets = this.Control.Move(srcdst.X, srcdst.Y,
                                     srcdst.XMore, srcdst.YMore);
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
            this.Say(message);
            return false;
          }
        }
        if (rets.Contains(Ret.Promote))
        {
          char prom = Regex.Replace(note.Substring(5), @"[*&#+:p%]",
                                    string.Empty)[0];
          this.Control.Promote(rets, prom,
                               srcdst.X, srcdst.Y,
                               srcdst.XMore, srcdst.YMore);
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

/**
    public Option
    Menu()
    {
      var menu = this.Es["Menu"];
      this.Erase();
      //menu.Draw(this, null);

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
            string style = Helper.JsonToValue(this.Cfg, "style");
            if (! this.GoodDimensions(style))
            {
              switch (style)
              {
                case "compact":
                  this.Say(Res.Menu, "Compact style needs at least 64 x 29", true);
                  break;
                case "wide":
                  this.Say(Res.Menu, "Wide style needs at least 26 x 19", true);
                  break;
              }
              //menu.Draw(this, null);
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
//*/

    // Game actions ////////////////////////////////////////////////////////////


    // Stop: Stop the game.

    public void
    Stop(string message)
    {
      if (Ask(message + " (y/n)", false))
      {
        if (File.Exists("chesh.log") &&
            Ask("chesh.log already exists. Sure? (y/n)", false))
        {
          this.UiState.Running = false;
          this.Control.Save();
          this.Say("Game log saved: chesh.log");
        }
      }
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
        this.Stop("Tied. Save before quitting?");
        Pin(0, Height + 2);
        return true;
      }
      if (ret == Ret.Tying)
      {
        if (this.Ask($"{this.Turn(false)} proposed a tie. Accept?", false))
        {
          return this.Tie();
        }
        this.Control.Untie();
        this.Say("Tie declined");
      }
      return false;
    }


    // Undo: Undo the last move.

    public bool
    Undo()
    {
      return this.Control.Undo();
    }
  }
}
