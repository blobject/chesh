using System.Collections.Generic;
using Chesh.Model;
using Chesh.Util;
using Chesh.View;

namespace Chesh.Controller
{

  // Control: An intermediary object that talks between the model (Game/State)
  //          and the view (Ui).
  //          Observes State and Cfg changes.

  public class Control : IObserver
  {
    private Game Game;
    private Ui Ui;

    public Control(Game game, Ui ui)
    {
      this.Game = game;
      this.Ui = ui;
    }


    // M -> C (-> set V) ///////////////////////////////////////////////////////

    // ChangeState: State change was observed; call Ui accordingly.

    public void
    ChangeState(State next)
    {
      this.Ui.SetState(Helper.ToJson(next));
      this.Ui.Erase();
      this.Ui.Draw();
    }


    // ChangeCfg: Cfg change was observed; call Ui accordingly.

    public void
    ChangeCfg(Dictionary<string,string> next)
    {
      string cfg = Helper.ToJson(next);
      this.Ui.SetCfg(cfg);
      foreach (KeyValuePair<string,Element> element in this.Ui.Es)
      {
        element.Value.SetCfg(cfg);
      }
      this.Ui.Erase();
      this.Ui.Es["Menu"].Draw(cfg);
    }


    // V -> C (-> set M) ///////////////////////////////////////////////////////

    // SetStyleCfg: Set Cfg.style

    public void
    SetStyleCfg(string style)
    {
      this.Game.SetStyleCfg(style);
    }


    // Save: Call Game.Save.

    public void
    Save()
    {
      this.Game.Save();
    }


    // Reset: Call Game.Reset.

    public void
    Reset()
    {
      this.Game.Reset();
    }


    // Tie: Call Game.Tie.

    public Ret
    Tie(string color)
    {
      return this.Game.Tie(color);
    }


    // Untie: Call Game.Untie.

    public void
    Untie()
    {
      this.Game.Untie();
    }


    // TODO: unused
    // Select: Call Game.Select.

    public Ret
    Select(int x, int y)
    {
      return this.Game.Select(x, y);
    }


    // Promote: Call Game.Promote.

    public void
    Promote(List<Ret> rets, char prom, int xSrc, int ySrc, int xDst, int yDst)
    {
      this.Game.Promote(rets, prom, xSrc, ySrc, xDst, yDst);
    }


    // Undo: Call Game.Undo.

    public bool
    Undo()
    {
      return this.Game.Undo();
    }


    // Move: Call Game.Move.

    public List<Ret>
    Move(int xSrc, int ySrc, int xDst, int yDst)
    {
      return this.Game.Move(xSrc, ySrc, xDst, yDst);
    }
  }
}
