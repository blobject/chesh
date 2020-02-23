using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using Chesh;
using Chesh.Controller;
using Chesh.Model;
using Chesh.View;
using Chesh.Util;

namespace Test
{
  class TestHelper
  {
    public static string
    CreateLog(string fileprefix, string content)
    {
      // for a quasi-unique filename
      string filename = fileprefix + DateTime.Now.ToString("yyMMddHHmmss") + ".log";
      using (var writer = new StreamWriter(filename))
      {
        using (var reader = new StringReader(content))
        {
          string line;
          while ((line = reader.ReadLine()) != null)
          {
            writer.WriteLine(line);
          }
        }
      }
      return filename;
    }

    public static void
    DeleteLog(string filename)
    {
      File.Delete(filename);
    }
  }

  public class ProgramTest
  {
    [Fact]
    public void ProgramInit()
    {
      string style;
      List<string> load;
      float speed;

      Assert.True(Program.Args(new string[] {},
                               out style, out load, out speed));
      Assert.Equal("compact", style);
      Assert.Empty(load);
      Assert.Equal(0, speed);

      Assert.True(Program.Args(new string[] { "w" },
                               out style, out load, out speed));
      Assert.Equal("wide", style);
      Assert.Empty(load);
      Assert.Equal(0, speed);

      Assert.False(Program.Args(new string[] { "foo" },
                                out style, out load, out speed));

      string log = TestHelper.CreateLog("test", "# a test game\nPa2a4 Pb7b5\nPa4b5:\n");

      Assert.True(Program.Args(new string[] { "c", log },
                               out style, out load, out speed));
      Assert.Equal("compact", style);
      Assert.Equal(3, load.Count);
      Assert.Equal("Pa2a4", load[0]);
      Assert.Equal(0, speed);

      Assert.True(Program.Args(new string[] { "c", log, ".5" },
                               out style, out load, out speed));
      Assert.Equal("compact", style);
      Assert.Equal(3, load.Count);
      Assert.Equal("Pa2a4", load[0]);
      Assert.Equal(0.5, speed);

      Assert.False(Program.Args(new string[] { "c", log, "foo" },
                                out style, out load, out speed));

      TestHelper.DeleteLog(log);
      Assert.False(Program.Args(new string[] { "c", log },
                                out style, out load, out speed));
    }
  }

  public class ModelTest
  {
    [Fact]
    public void GameInit()
    {
      Game o = new Game("compact", null);
      Assert.NotNull(o);
      Assert.False(o.BlackTie);
      Assert.False(o.WhiteTie);
    }

    [Fact]
    public void GameTie()
    {
      Game o = new Game("compact", null);
      Assert.NotNull(o);
      Assert.False(o.BlackTie);
      Assert.False(o.WhiteTie);

      Assert.Equal(Ret.Tying, o.Tie("Black"));
      Assert.True(o.BlackTie);
      Assert.False(o.WhiteTie);

      o.Untie();
      Assert.False(o.BlackTie);
      Assert.False(o.WhiteTie);

      Assert.Equal(Ret.Tying, o.Tie("White"));
      Assert.False(o.BlackTie);
      Assert.True(o.WhiteTie);

      Assert.Equal(Ret.Tie, o.Tie("Black"));
      Assert.True(o.BlackTie);
      Assert.True(o.WhiteTie);

      o.Reset();
      Assert.False(o.BlackTie);
      Assert.False(o.WhiteTie);
    }

    [Fact]
    public void StateInit()
    {
      State o = new State(null);
      Assert.NotNull(o);
      Assert.Equal(32, o.Live.Count);
      Assert.Empty(o.Dead);
      Assert.Empty(o.History);
      Assert.All(o.Live, entry =>
      {
        Assert.Contains(entry.Sym.ToLower(),
                        (new List<string>() {"o","p","r","n","b","q","k"}));
        Assert.True(entry.Color == Color.Black || entry.Color == Color.White);
        Assert.True(entry.X >= 1 && entry.X <= 8);
        Assert.True(entry.Y >= 1 && entry.Y <= 8);
        Assert.True(entry.Inert);
      });
    }

    [Fact]
    public void StateCustomInit() // for testing other things
    {
      State o = new State("wnc3");
      Assert.NotNull(o);
      Assert.Single(o.Live);
      Assert.Empty(o.Dead);
      Assert.Empty(o.History);
      Assert.Equal("N", o.At(o.Live, 3, 3).Sym);
      Assert.Equal(Color.White, o.At(o.Live, 3, 3).Color);

      o = new State("wpa1 brb2 wnc3 bbd4 wqe5 bkf6");
      Assert.NotNull(o);
      Assert.Equal(6, o.Live.Count);
      Assert.Equal("P", o.At(o.Live, 1, 1).Sym);
      Assert.Equal("r", o.At(o.Live, 2, 2).Sym);
      Assert.Equal("N", o.At(o.Live, 3, 3).Sym);
      Assert.Equal("b", o.At(o.Live, 4, 4).Sym);
      Assert.Equal("Q", o.At(o.Live, 5, 5).Sym);
      Assert.Equal("k", o.At(o.Live, 6, 6).Sym);
    }

    [Fact]
    public void StateAccess()
    {
      State o = new State(null);
      Assert.Equal("q", o.At(o.Live, 4, 8).Sym);
      Assert.Equal(Color.White, o.At(o.Live, 5, 1).Color);
      Assert.True(o.At(o.Live, 7, 7).Inert);

      Assert.Equal(Color.White, o.Turn(true));
      Assert.Equal(Color.Black, o.Turn(false));

      Piece blackKing = o.King(Color.Black, true);
      Assert.Equal(Color.Black, blackKing.Color);
      Assert.Equal(5, blackKing.X);
      Assert.Equal(8, blackKing.Y);

      o = new State("bbc3 wnf6");
      List<(int,int,int,int)> r = o.LineOfAttack(o.At(o.Live, 3, 3),
                                                 o.At(o.Live, 6, 6));
      Assert.Contains((3, 3, 3, 3), r);
      Assert.Contains((4, 4, 0, 0), r);
      Assert.Contains((5, 5, 0, 0), r);
    }

    [Fact]
    public void StateKill()
    {
      State o = new State(null);
      o.Kill(1, 1);
      Assert.Null(o.At(o.Live, 1, 1));
    }

    [Fact]
    public void StateChecking()
    {
      State o = new State("bkd6 wrc2");
      Assert.True(o.WillCheck(Color.White, o.At(o.Live, 3, 2), 4, 2));
      o = new State("brc6 wqc4 wkc2");
      Assert.True(o.WillBeChecked(Color.White, o.At(o.Live, 3, 4), 2, 4));
      o = new State("wka1 bqh2 bnc4");
      Assert.True(o.WillCheckmate(Color.Black, o.At(o.Live, 8, 2), 2, 2));
    }

    [Fact]
    public void PieceAccess()
    {
      State o = new State("wrd4 bpd7");
      Piece p = o.At(o.Live, 4, 4);

      Assert.True(Piece.Is(p, "Rook"));

      List<(int,int,int,int)> r1 = p.Reach(o.Board, null);
      Assert.Equal(13, r1.Count);
      Assert.Contains((4, 5, 0, 0), r1);
      Assert.Contains((4, 6, 0, 0), r1);
      Assert.Contains((4, 7, 4, 7), r1);
      Assert.Contains((5, 4, 0, 0), r1);
      Assert.Contains((6, 4, 0, 0), r1);
      Assert.Contains((7, 4, 0, 0), r1);
      Assert.Contains((8, 4, 0, 0), r1);
      Assert.Contains((4, 3, 0, 0), r1);
      Assert.Contains((4, 2, 0, 0), r1);
      Assert.Contains((4, 1, 0, 0), r1);
      Assert.Contains((3, 4, 0, 0), r1);
      Assert.Contains((2, 4, 0, 0), r1);
      Assert.Contains((1, 4, 0, 0), r1);

      int x;
      int y;
      o = new State("wke1 wra1");
      p = o.At(o.Live, 5, 1);
      List<Ret> r2 = p.Castle(o, 3, 1, out x, out y);
      Assert.Contains(Ret.Castle, r2);

      o = new State("wke1 wra1 brb8");
      p = o.At(o.Live, 5, 1);
      r2 = p.Castle(o, 3, 1, out x, out y);
      Assert.Contains(Ret.BadCastle, r2);
    }
  }

  public class MoveTest
  {
    [Fact]
    public void StateMove()
    {
      State o = new State(null);
      Assert.Empty(o.Move(1, 2, 1, 3));
      Assert.Equal("Pa2a3", o.LastNote());
      Assert.Contains(Ret.BadTurn, o.Move(1, 3, 1, 4));
      Assert.Empty(o.Move(4, 7, 4, 5));
      Assert.Contains(Ret.BadSrc, o.Move(3, 3, 4, 4));
      Assert.Contains(Ret.BadDst, o.Move(6, 1, 5, 2));
      Assert.Contains(Ret.InvalidMove, o.Move(5, 1, 5, 3));
      o.Move(3, 2, 3, 4);
      Assert.Contains(Ret.Capture, o.Move(4, 5, 3, 4));
      o.Move(2, 2, 2, 4);
      Assert.Contains(Ret.EnPassant, o.Move(3, 4, 2, 3));
      Assert.Equal("Pc4b3p", o.LastNote());
    }

    [Fact]
    public void StatePromote()
    {
      // enemy king needed for extended calculation of promotion
      State o = new State("wpb7 bka8");
      Assert.Equal(2, o.Live.Count);
      Assert.Equal("P", o.Live[0].Sym);
      Assert.Equal("k", o.Live[1].Sym);
      o.Promote(new List<Ret>(), 'Q', 2, 7, 2, 8);
      Assert.Equal(2, o.Live.Count);
      Assert.Equal("k", o.Live[0].Sym);
      Assert.Equal("Q", o.Live[1].Sym);
    }

    [Fact]
    public void StateUndo()
    {
      State o = new State(null);
      Assert.Empty(o.Move(1, 2, 1, 4));
      Assert.Null(o.At(o.Live, 1, 2));
      Assert.NotNull(o.At(o.Live, 1, 4));
      Assert.False(o.At(o.Live, 1, 4).Inert);
      Assert.Single(o.History);
      o.Undo(o.LastNote());
      Assert.Null(o.At(o.Live, 1, 4));
      Assert.NotNull(o.At(o.Live, 1, 2));
      Assert.True(o.At(o.Live, 1, 2).Inert);
      Assert.Empty(o.History);
      Assert.Empty(o.Move(1, 2, 1, 4));
      Assert.Single(o.History);
    }
  }

  public class ViewTest
  {
    [Fact]
    public void UiInit()
    {
      Ui o = new Ui(new Game("compact", null));
      Assert.NotNull(o);
      Assert.True(o.Es.ContainsKey("Menu"));
      Assert.True(o.Es.ContainsKey("BlackResponse"));
      Assert.True(o.Es.ContainsKey("BlackPrompt"));
      Assert.True(o.Es.ContainsKey("History"));
      Assert.True(o.Es.ContainsKey("WhiteDead"));
      Assert.True(o.Es.ContainsKey("Frame"));
      Assert.True(o.Es.ContainsKey("Pieces"));
      Assert.True(o.Es.ContainsKey("BlackDead"));
      Assert.True(o.Es.ContainsKey("WhiteResponse"));
      Assert.True(o.Es.ContainsKey("WhitePrompt"));
    }

    [Fact]
    public void UiMethods()
    {
      Ui o = new Ui(new Game("compact", null));

      Assert.Equal("White", o.Turn(true));
      Assert.Equal("Black", o.Turn(false));

      Assert.Equal("Queen", o.At('d', '8'));
      Assert.Equal("King", o.At('e', '1'));
      Assert.Equal("Pawn", o.At('g', '7'));
    }
  }

  public class MvcTest
  {
    [Fact]
    public void GameMove()
    {
      Game g = new Game("compact", null);
      Ui u = new Ui(g);
      Control o = new Control(g, u);
      g.Attach(o);
      u.SetControl(o);

      //g.Move(1, 2, 1, 3); // advance first white pawn
      // how to test view without triggering draw?
    }

    // TODO: test these:
    // Game.SetStyleCfg
    // Game.Undo
    // Game.Promote
    // Game.Move
  }
}
