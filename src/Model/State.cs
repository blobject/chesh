using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Chesh.Util;

namespace Chesh.Model
{

  // State: The data needed to recreate the game.
  //        A member of the main Game object.
  //        .Board is a computation helper.

  public class State
  {
    public char[,] Board;
    public Piece Selection { get; set; }
    public List<Swap> Reach { get; set; }
    public List<Piece> Live { get; set; }
    public List<Piece> Dead { get; set; }
    public List<(string,long)> History { get; set; }

    public State(string strands)
    {
      var live = new List<Piece>();
      if (strands == null)
      {
        live.Add(new Rook(Color.White, 1, 1));
        live.Add(new Knight(Color.White, 2, 1));
        live.Add(new Bishop(Color.White, 3, 1));
        live.Add(new Queen(Color.White, 4, 1));
        live.Add(new King(Color.White, 5, 1));
        live.Add(new Bishop(Color.White, 6, 1));
        live.Add(new Knight(Color.White, 7, 1));
        live.Add(new Rook(Color.White, 8, 1));
        for (int file = 1; file <= 8; file++)
        {
          live.Add(new Pawn(Color.White, file, 2));
        }
        for (int file = 1; file <= 8; file++)
        {
          live.Add(new Pawn(Color.Black, file, 7));
        }
        live.Add(new Rook(Color.Black, 1, 8));
        live.Add(new Knight(Color.Black, 2, 8));
        live.Add(new Bishop(Color.Black, 3, 8));
        live.Add(new Queen(Color.Black, 4, 8));
        live.Add(new King(Color.Black, 5, 8));
        live.Add(new Bishop(Color.Black, 6, 8));
        live.Add(new Knight(Color.Black, 7, 8));
        live.Add(new Rook(Color.Black, 8, 8));
      }
      else
      {
        foreach(var piece in Helper.StrandsToPieces(strands))
        {
          live.Add(piece);
        }
      }
      this.Live = live;
      this.Dead = new List<Piece>();
      this.History = new List<(string,long)>();
      this.UpdateBoard(out this.Board);
    }


    // accessors ///////////////////////////////////////////////////////////////

    // At: Get the piece on specified square.

    public Piece
    At(List<Piece> pieces, int x, int y)
    {
      Piece at = null;
      foreach (var piece in pieces)
      {
        if (piece.X == x && piece.Y == y)
        {
          at = piece;
          // don't break; we want the last one in the case of State.Dead
        }
      }
      return at;
    }


    // Turn: Get the color of current (or the other) player's turn.
    //       Based only on the parity of the number of entries in the history.

    public Color
    Turn(bool current)
    {
      if (this.History.Count % 2 == 0)
      {
        if (current)
        {
          return Color.White;
        }
        return Color.Black;
      }
      if (current)
      {
        return Color.Black;
      }
      return Color.White;
    }


    // Other: Get the color.

    public Color
    Other(Color color)
    {
      Color other = Color.Black;
      if (color == other)
      {
        other = Color.White;
      }
      return other;
    }


    // LastNote: Get the move notation from the last entry in the history.

    public string
    LastNote()
    {
      if (this.History.Count > 0)
      {
        return this.History[this.History.Count - 1].Item1;
      }
      return null;
    }


    // King: Get the King piece, either enemy or friendly.

    public Piece
    King(Color turn, bool friendly)
    {
      Piece king = null;
      foreach (var piece in this.Live)
      {
        if (friendly && piece.Color != turn)
        {
          continue;
        }
        if (! friendly && piece.Color == turn)
        {
          continue;
        }
        if (Piece.Is(piece, "King"))
        {
          king = piece;
          break;
        }
      }
      return king;
    }


    // LineOfAttack: Get the squares from attacker to target, including
    //               the attacker, excluding the target.

    public List<Swap>
    LineOfAttack(Piece attacker, Piece target)
    {
      List<Swap> ray = null;
      var targetSwap = new Swap(target.X, target.Y, target.X, target.Y);
      string last = this.LastNote();

      // find the right ray first
      foreach (var threat in attacker.Rays(this, last))
      {
        if (threat.Contains(targetSwap))
        {
          ray = threat;
          break;
        }
      }

      // remove the target from, but add the attacker to, the ray
      ray.Remove(targetSwap);
      ray.Add(new Swap(attacker.X, attacker.Y, attacker.X, attacker.Y));
      return ray;
    }


    // mutators ////////////////////////////////////////////////////////////////

    // UpdateBoard: Update .Board out of the live pieces.

    public void
    UpdateBoard(out char[,] board)
    {
      // this should not be called outside of the move/promotion process
      board = new char[8, 8];
      foreach (var piece in this.Live)
      {
        board[piece.X - 1, piece.Y - 1] = piece.Sym[0];
      }
    }


    // Tie: Record that a player proposed a draw.

    public void
    Tie()
    {
      // mutate state
      this.History.Add(("tie", (new DateTime(DateTime.Now.Ticks)).Ticks));
    }


    // Untie: Record that a player declined the draw proposal.

    public void
    Untie()
    {
      // mutate state
      this.History.Add(("nope", (new DateTime(DateTime.Now.Ticks)).Ticks));
    }


    // Kill: Remove the piece from the set of live pieces.

    public void
    Kill(int x, int y)
    {
      this.Live.Remove(this.At(this.Live, x, y));
    }


    // player actions //////////////////////////////////////////////////////////

    // Select: Set State.Selection.

    public void
    Select(int x, int y)
    {
      Piece piece = this.At(this.Live, x, y);
      if (piece == null || piece.Color != this.Turn(true))
      {
        this.Selection = null;
        this.Reach = new List<Swap>();
        return;
      }
      this.Selection = piece;
      this.Reach = piece.Reach(this, this.LastNote());
    }


    // Probe: Tentatively move, for easier discernment of movement type.

    public void
    Probe(Piece piece, int x, int y)
    {
      piece.X = x;
      piece.Y = y;
      this.UpdateBoard(out this.Board);
    }


    // WillBeChecked: Determine if a move will lead to being checked.

    public bool
    WillBeChecked(Color turn, Piece mover, int x, int y)
    {
      bool check = false;
      int xOld = mover.X;
      int yOld = mover.Y;
      this.Probe(mover, x, y); // tentative move
      this.Board[x - 1, y - 1] = mover.Sym[0]; // in case of capture

      // will the king be threatened?
      string last = this.LastNote();
      Piece king = this.King(turn, true);
      foreach (var enemy in this.Live)
      {
        // consider the reach of enemies only
        if (enemy.Color == turn)
        {
          continue;
        }
        // disregard any captured piece
        if (enemy.X == x && enemy.Y == y)
        {
          continue;
        }
        foreach (var swap in enemy.Reach(this, last))
        {
          if (swap.X == king.X && swap.Y == king.Y)
          {
            check = true;
            break;
          }
        }
        if (check)
        {
          break;
        }
      }

      this.Probe(mover, xOld, yOld); // backtrack
      return check;
    }


    // WillCheck: Determine if a move will check.

    public bool
    WillCheck(Color turn, Piece mover, int x, int y)
    {
      bool check = false;
      int xOld = mover.X;
      int yOld = mover.Y;
      this.Probe(mover, x, y); // tentative move
      this.Board[x - 1, y - 1] = mover.Sym[0]; // in case of more than one

      // will the move threaten the enemy king?
      Piece enemyKing = this.King(turn, false);
      foreach (var swap in mover.Reach(this, this.LastNote()))
      {
        if (swap.X == enemyKing.X && swap.Y == enemyKing.Y)
        {
          check = true;
        }
      }

      this.Probe(mover, xOld, yOld); // backtrack
      return check;
    }


    // WillCheckmate: Determine if a move will checkmate.

    public bool
    WillCheckmate(Color turn, Piece mover, int x, int y)
    {
      bool checkmate = true;
      int xOld = mover.X;
      int yOld = mover.Y;
      this.Probe(mover, x, y); // tentative move
      this.Board[x - 1, y - 1] = mover.Sym[0]; // in case of capture

      // can the enemy king move somewhere safe?
      Piece enemyKing = this.King(turn, false);
      foreach (var swap in enemyKing.Reach(this, null))
      {
        if (! this.WillBeChecked(this.Other(turn), enemyKing, swap.X, swap.Y))
        {
          checkmate = false;
          break;
        }
      }

      // get line of attack - we do this here, before backtracking
      List<Swap> threat = null;
      if (checkmate)
      {
        threat = this.LineOfAttack(mover, enemyKing);
      }

      this.Probe(mover, xOld, yOld); // backtrack before possible return

      if (! checkmate)
      {
        return false;
      }

      this.Probe(mover, x, y); // tentative move
      this.Board[x - 1, y - 1] = mover.Sym[0]; // in case of capture

      // can any friendly block the threat?
      string last = this.LastNote();
      foreach (var swap in threat)
      {
        foreach (var friendly in this.Live)
        {
          // consider friendlies only
          if (friendly.Color == turn)
          {
            continue;
          }
          // disregard any captured piece
          if (friendly.X == x && friendly.Y == y)
          {
            continue;
          }
          foreach (var place in friendly.Reach(this, last))
          {
            if (place.Equals(swap) &&
                ! this.WillBeChecked(this.Other(turn), friendly,
                                     place.X, place.Y))
            {
              checkmate = false;
              break;
            }
          }
        }
        if (! checkmate)
        {
          break;
        }
      }

      this.Probe(mover, xOld, yOld); // backtrack
      return checkmate;
    }


    // Promote: Promote a pawn.
    //          Mutates State via Upstate.

    public void
    Promote(List<Ret> rets, char prom, int xSrc, int ySrc, int xDst, int yDst)
    {
      Color turn = this.Turn(true);
      Piece promoted;
      if (prom == 'R')
      {
        promoted = new Rook(turn, xDst, yDst);
      }
      else if (prom == 'N')
      {
        promoted = new Knight(turn, xDst, yDst);
      }
      else if (prom == 'B')
      {
        promoted = new Bishop(turn, xDst, yDst);
      }
      else // 'Q
      {
        promoted = new Queen(turn, xDst, yDst);
      }

      // the promotion is also a capture, so kill the enemy
      if (rets.Contains(Ret.Capture))
      {
        this.Dead.Add(this.At(this.Live, xDst, yDst));
        this.Kill(xDst, yDst);
      }

      // remove the pawn and add the newly promoted
      this.Kill(xSrc, ySrc);
      this.Live.Add(promoted);

      // does the promotion result in check or checkmate?
      if (this.WillCheck(turn, promoted, xDst, yDst))
      {
        rets.Add(Ret.Check);
        if (this.WillCheckmate(turn, promoted, xDst, yDst))
        {
          rets.Remove(Ret.Check);
          rets.Add(Ret.Checkmate);
        }
      }

      // mutate state
      this.Upstate(rets, promoted, "P", prom, xSrc, ySrc, xDst, yDst, 0, 0);
    }


    // UndoPromote: Part of Undo, separated out for reuse.
    //              Mutates State.

    public bool
    UndoPromote(string suffix, Piece promoted, Color color,
                int xSrc, int ySrc, int yDst)
    {
      // kill the newly promoted, create a new quickened pawn and make it live
      if (Regex.IsMatch(suffix, @"[RNBQ]"))
      {
        int y = yDst - 1;
        if (color == Color.Black)
        {
          y = yDst + 1;
        }
        this.Live.Remove(promoted);
        var pawn = new Pawn(color, xSrc, ySrc);
        pawn.Inert = false;
        this.Live.Add(pawn);
        return true;
      }
      return false;
    }


    // Undo: Undo the last move.
    //       Mutates State.

    public void
    Undo(string last)
    {
      string src = last.Substring(1, 2);
      int xSrc = Helper.ToFileNum(src[0]);
      int ySrc = Helper.ToRankNum(src[1]);
      string dst = last.Substring(3, 2);
      int xDst = Helper.ToFileNum(dst[0]);
      int yDst = Helper.ToRankNum(dst[1]);
      string suffix = last.Substring(5);
      Piece piece = this.At(this.Live, xDst, yDst);
      Color color = Color.Black;
      if (this.History.Count % 2 == 1)
      {
        color = Color.White;
      }

      // FIX: fix: "Q:" then "Q", pawn persists

      // inert: determine inertness by tracing the path of movement in history
      var path = new List<string>();
      string node = src;
      string note;
      for (int i = this.History.Count - 1; i >= 0; i--)
      {
        if (color == Color.Black && i % 2 == 0)
        {
          continue;
        }
        if (color == Color.White && i % 2 == 1)
        {
          continue;
        }
        note = this.History[i].Item1;
        if (note.Substring(3, 2) == node)
        {
          node = note.Substring(1, 2);
          path.Add(note);
        }
      }
      if (path.Count == 0) // no prior move to src square, ie. was inert
      {
        piece.Inert = true;
      }

      // regular move: just reset the mover's position
      if (suffix == string.Empty)
      {
        piece.X = xSrc;
        piece.Y = ySrc;
      }

      // capturing: retrieve the captured piece and reinstate it, while
      //            resetting the captor's position
      if (Regex.IsMatch(suffix, @"[&*:]"))
      {
        Piece reborn = this.At(this.Dead, xDst, yDst);
        this.Dead.Remove(reborn);
        this.Live.Add(reborn);

        // and also promoting?
        if (! this.UndoPromote(suffix, piece, color, xSrc, ySrc, yDst))
        {
          piece.X = xSrc;
          piece.Y = ySrc;
        }
      }

      // castling: reset positions of both king and rook
      if (suffix.Contains('%'))
      {
        piece.X = xSrc;
        if (xDst < 5) // queenside
        {
          this.At(this.Live, xDst + 1, ySrc).X = 1;
        }
        else
        {
          this.At(this.Live, xDst + 1, ySrc).X = 8;
        }
      }

      // en passant: pretty much like capturing
      if (suffix.Contains('p'))
      {
        int y = yDst - 1;
        if (color == Color.Black)
        {
          y = yDst + 1;
        }
        Piece reborn = this.At(this.Dead, xDst, y);
        this.Dead.Remove(reborn);
        this.Live.Add(reborn);
        piece.X = xSrc;
        piece.Y = ySrc;
      }

      // promoting
      this.UndoPromote(suffix, piece, color, xSrc, ySrc, yDst);

      // remove last entry from the history
      this.History.RemoveAt(this.History.Count - 1);

      this.UpdateBoard(out this.Board);
    }


    // Move: Move a piece.
    //       Mutates State via Upstate.

    public List<Ret>
    Move(int xSrc, int ySrc, int xDst, int yDst)
    {
      Piece src = this.At(this.Live, xSrc, ySrc);
      Piece dst = this.At(this.Live, xDst, yDst);

      // get simple bad moves out of the way
      if (src == null)
      {
        return new List<Ret>() { Ret.BadSrc };
      }
      if (src.Color != this.Turn(true))
      {
        return new List<Ret>() { Ret.BadTurn };
      }
      if (dst != null && dst.Color == this.Turn(true))
      {
        return new List<Ret>() { Ret.BadDst };
      }

      // try making the move
      // note: this is the first creation of <Ret>s
      var rets = src.Move(this, xDst, yDst);

      // bad moves that are a bit more involved
      // note: original <Ret>s can be ignored
      if (rets.Contains(Ret.InvalidMove))
      {
        return rets;
      }
      if (this.WillBeChecked(this.Turn(true), src, xDst, yDst))
      {
        return new List<Ret>() { Ret.Checked };
      }

      // good moves
      // note: must append to the first <Ret>s
      if (Piece.Is(src, "Pawn") &&
          ((src.Color == Color.Black && yDst == 1) ||
           (src.Color == Color.White && yDst == 8)))
      {
        rets.Add(Ret.Promote);
        return rets;
      }
      if (this.WillCheck(this.Turn(true), src, xDst, yDst))
      {
        rets.Add(Ret.Check);
        if (this.WillCheckmate(this.Turn(true), src, xDst, yDst))
        {
          rets.Remove(Ret.Check);
          rets.Add(Ret.Checkmate);
        }
      }

      // "more" coordinates
      int xMore = 0;
      int yMore = 0;
      foreach (var swap in this.Reach)
      {
        if (xDst == swap.X && yDst == swap.Y)
        {
          xMore = swap.XMore;
          yMore = swap.YMore;
        }
      }

      // mutate state
      this.Upstate(rets, src, src.Sym, '\0', xSrc, ySrc,
                   xDst, yDst, xMore, yMore);
      return rets;
    }


    // Upstate: Helper that mutates the State after a move.

    public void
    Upstate(List<Ret> rets,
            Piece src, string sym, char prom,
            int xSrc, int ySrc,
            int xDst, int yDst,
            int xMore, int yMore)
    {
      src.Inert = false;
      this.Selection = null;
      // either capturing (or enpassant) or casting was involved
      if (xMore > 0 && yMore > 0)
      {
        if (rets.Contains(Ret.Capture) || rets.Contains(Ret.EnPassant))
        {
          this.Dead.Add(this.At(this.Live, xMore, yMore));
          this.Kill(xMore, yMore);
        }
        if (rets.Contains(Ret.Castle))
        {
          Piece rook = this.At(this.Live, xMore, yMore);
          rook.X = xDst - 1;
          if (xDst < 5) // queenside
          {
            rook.X = xDst + 1;
          }
        }
      }
      src.X = xDst;
      src.Y = yDst;
      this.History.Add((Helper.Notate(rets, sym, prom,
                                      xSrc, ySrc, xDst, yDst),
                        (new DateTime(DateTime.Now.Ticks)).Ticks));
      this.UpdateBoard(out this.Board);
    }
  }
}
