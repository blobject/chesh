using System.Collections.Generic;
using Chesh.Util;

namespace Chesh.Model
{

  // Color: A property of the chess pieces.
  //        Also used as an indicator of whose turn it is.

  public enum Color { Black, White }


  // Ret: The return value of a move.
  //      This gets passed from Model to View.

  public enum Ret
  {
    Selected,
    Tie,
    Tying,
    Castle,
    Capture,
    Promote,
    EnPassant,
    Check,
    Checkmate,
    Checked,
    BadSrc,
    BadDst,
    BadTurn,
    BadCastle,
    InvalidMove,
  }


  // Game: The main game object.
  //       Includes the observed State data, the observed Cfg data, as well as
  //       additional data needed for a complete chess game experience.

  public class Game : ISubject
  {
    private List<IObserver> _observers;
    private State _state;
    public State State
    {
      get
      {
        return _state;
      }
      set
      {
        StateChanged(value);
        _state = value;
      }
    }
    private Dictionary<string,string> _cfg;
    public Dictionary<string,string> Cfg
    {
      get
      {
        return _cfg;
      }
      set
      {
        CfgChanged(value);
        _cfg = value;
      }
    }
    public bool BlackTie;
    public bool WhiteTie;

    public Game(string style, string strands) // "string pieces"
    {
      _observers = new List<IObserver>();
      this.State = new State(strands);
      this.BlackTie = false;
      this.WhiteTie = false;
      this.Cfg = new Dictionary<string,string>();
      this.Cfg["style"] = style;
    }


    // mutators ////////////////////////////////////////////////////////////////

    // Attach: Add to list of objects observing State and Cfg.

    public void
    Attach(IObserver observer)
    {
      _observers.Add(observer);
    }


    // M -> notify C ///////////////////////////////////////////////////////////

    // StateChanged: Called when State is assigned.

    public void
    StateChanged(State next)
    {
      foreach (var observer in _observers)
      {
        observer.ChangeState(next);
      }
    }

    // CfgChanged: Called when Cfg is assigned.

    public void
    CfgChanged(Dictionary<string,string> next)
    {
      foreach (var observer in _observers)
      {
        observer.ChangeCfg(next);
      }
    }


    // player actions //////////////////////////////////////////////////////////

    // SetStyleCfg: Set the style.

    public void
    SetStyleCfg(string choice)
    {
      this.Cfg["style"] = choice;
      this.Cfg = this.Cfg; // trigger the observer
    }


    // Save: Call Program.Save

    public void
    Save()
    {
      Program.Save(this.State.History);
    }


    // Reset: Reset the game and trigger State observation.

    public void
    Reset()
    {
      this.BlackTie = false;
      this.WhiteTie = false;
      this.State = new State(null); // triggers the observer
    }


    // Tie: Set tie variables, Call State.Tie, and trigger State observation.

    public Ret
    Tie(string color)
    {
      Ret ret = Ret.Tying;
      if (color == "Black")
      {
        this.BlackTie = true;
        if (this.WhiteTie)
        {
          ret = Ret.Tie;
        }
      }
      else
      {
        this.WhiteTie = true;
        if (this.BlackTie)
        {
          ret = Ret.Tie;
        }
      }
      this.State.Tie();
      this.State = this.State; // trigger the observer
      return ret;
    }


    // Untie: Reset tie variables, Call State.Untie, and trigger State
    //        observation.

    public void
    Untie()
    {
      this.BlackTie = false;
      this.WhiteTie = false;
      this.State.Untie();
      this.State = this.State; // trigger the observer
    }


    // TODO: unused by current View
    // Select: Call State.Select.

    public Ret
    Select(int x, int y)
    {
      return this.State.Select(x, y);
    }


    // Promote: Call State.Promote and trigger State observation.

    public void
    Promote(List<Ret> rets, char prom, int xSrc, int ySrc, int xDst, int yDst)
    {
      this.State.Promote(rets, prom, xSrc, ySrc, xDst, yDst);
      this.State = this.State; // trigger the observer
    }


    // Undo: Call State.Undo and trigger State observation.

    public bool
    Undo()
    {
      string last = this.State.LastNote();
      if (last == null)
      {
        return false;
      }
      this.State.Undo(last);
      this.State = this.State; // trigger the observer
      return true;
    }


    // Move: Call State.Move and trigger State observation.

    public List<Ret>
    Move(int xSrc, int ySrc, int xDst, int yDst)
    {
      var rets = this.State.Move(xSrc, ySrc, xDst, yDst);
      this.State = this.State; // trigger the observer
      return rets;
    }
  }
}
