using System;
using System.Collections.Generic;
using Chesh.Util;

namespace Chesh.Model
{

  // Swap: A quadruple-like object representing the move-square and its attached
  //       swapping. This belongs to the reach of a piece.

  public class Swap
  {
    public int X { get; set; }
    public int Y { get; set; }
    public int XMore { get; set; }
    public int YMore { get; set; }

    public Swap(int x, int y, int xm, int ym)
    {
      this.X = x;
      this.Y = y;
      this.XMore = xm;
      this.YMore = ym;
    }

    public override bool
    Equals(object o)
    {
      var swap = o as Swap;
      if (swap == null)
      {
        return false;
      }
      return swap.X == this.X && swap.Y == this.Y &&
        swap.XMore == this.XMore && swap.YMore == this.YMore;
    }

    public override int
    GetHashCode()
    {
      return new { this.X, this.Y, this.XMore, this.YMore }.GetHashCode();
    }
  }

  // Piece: The object representing a movable chess piece.

  public abstract class Piece
  {
    public string Sym { get; set; }
    public Color Color { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Inert { get; set; }

    public Piece(string sym, Color color, int x, int y)
    {
      this.Sym = sym;
      this.Color = color;
      if (color == Color.Black)
      {
        this.Sym = sym.ToLower();
      }
      this.X = x;
      this.Y = y;
      this.Inert = true;
    }


    // Quicken: Claim that the piece has moved at least once.

    public void
    Quicken()
    {
      this.Inert = false;
    }


    // Is: Convenience method to check piece type.

    public static bool
    Is(Piece piece, string name)
    {
      return piece.GetType().Name == name;
    }


    // Rays: Lists linear moves in all possible directions.

    public abstract List<List<Swap>>
    Rays(State state, string last);


    // Reach: Flattened Rays.

    public virtual List<Swap>
    Reach(State state, string last)
    {
      var reach = new List<Swap>();
      foreach (var ray in this.Rays(state, last))
      {
        reach.AddRange(ray);
      }
      return reach;
    }


    // stub: implemented only by King
    public virtual List<Ret>
    Castle(State state, int x, int y, out int xMore, out int yMore)
    {
      xMore = 0;
      yMore = 0;
      return null;
    }


    // stub: implemented only by Pawn
    public virtual List<Swap>
    EnPassant(State state, string last, int x, int y)
    {
      return null;
    }


    // Move: Move the piece.

    public List<Ret>
    Move(State state, int x, int y)
    {
      int xMore;
      int yMore;
      foreach (var swap in this.Reach(state, state.LastNote()))
      {
        if (swap.X == x && swap.Y == y)
        {
          xMore = swap.XMore;
          yMore = swap.YMore;
          if (xMore > 0 && yMore > 0 &&
              Helper.CanCapture(this.Color, state.Board[xMore - 1, yMore - 1]))
          {
            if (xMore == x && yMore == y)
            {
              return new List<Ret>() { Ret.Capture };
            }
            return new List<Ret>() { Ret.EnPassant };
          }
          if (Is(this, "King") && (int) Math.Abs(this.X - x) == 2)
          {
            return new List<Ret>() { Ret.Castle };
          }
          return new List<Ret>();
        }
      }
      return new List<Ret>() { Ret.InvalidMove };
    }
  }

  public class Pawn : Piece
  {
    public Pawn(Color color, int x, int y) : base("P", color, x, y) {}


    // EnPassant: Determine possibility of en passant moves.

    public override List<Swap>
    EnPassant(State state, string last, int x, int y)
    {
      if (last != null && last[0] == 'P')
      {
        bool ep = false;
        bool black = char.IsLower(last[0]);
        Swap enemy = Helper.MoveToSwap(last.Substring(1));
        if (this.Color == Color.Black &&
            this.Y == 4 && x == enemy.XMore &&
            enemy.Y == y - 1 && enemy.YMore == y + 1)
        {
          ep = true;
        }
        else if (this.Y == 5 && x == enemy.XMore &&
                 enemy.Y == y + 1 && enemy.YMore == y - 1)
        {
          ep = true;
        }
        if (ep && Helper.CanCapture(this.Color,
                                    state.Board[x - 1, enemy.YMore - 1]))
        {
          return new List<Swap>() { new Swap(x, y, x, enemy.YMore) };
        }
      }
      return null;
    }

    public override List<List<Swap>>
    Rays(State state, string last)
    {
      var rays = new List<List<Swap>>();
      int x;
      int y;

      // advance one
      y = this.Y + 1;
      if (this.Color == Color.Black)
      {
        y = this.Y - 1;
      }
      if (y < 1 || y > 8)
      {
        return rays;
      }
      if (state.Board[this.X - 1, y - 1] == 0)
      {
        rays.Add(new List<Swap>() { new Swap(this.X, y, 0, 0) });
      }

      // capture
      List<Swap> epSwap;
      x = this.X - 1;
      if (x >= 1)
      {
        if (Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
        {
          rays.Add(new List<Swap>() { new Swap(x, y, x, y) });
        }

        // en passant
        epSwap = this.EnPassant(state, last, x, y);
        if (epSwap != null)
        {
          rays.Add(epSwap);
        }
      }
      x = this.X + 1;
      if (x <= 8)
      {
        if (Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
        {
          rays.Add(new List<Swap>() { new Swap(x, y, x, y) });
        }

        // en passant
        epSwap = this.EnPassant(state, last, x, y);
        if (epSwap != null)
        {
          rays.Add(epSwap);
        }
      }

      // advance two
      if (! this.Inert)
      {
        return rays;
      }
      y = this.Y + 2;
      if (this.Color == Color.Black)
      {
        y = this.Y - 2;
      }
      if (y < 1 || y > 8)
      {
        return rays;
      }
      if (rays.Count > 0 && state.Board[this.X - 1, y - 1] == 0)
      {
        rays.Add(new List<Swap>() { new Swap(this.X, y, 0, 0) });
      }

      return rays;
    }
  }

  public class Rook : Piece
  {
    public Rook(Color color, int x, int y) : base("R", color, x, y) {}

    public override List<List<Swap>>
    Rays(State state, string unused)
    {
      var rays = new List<List<Swap>>();
      int x;
      int y;
      List<Swap> ray;

      // north
      ray = new List<Swap>();
      y = this.Y + 1;
      while (y <= 8 && state.Board[this.X - 1, y - 1] == 0)
      {
        ray.Add(new Swap(this.X, y, 0, 0));
        y++;
      }
      if (y <= 8 && Helper.CanCapture(this.Color,
                                      state.Board[this.X - 1, y - 1]))
      {
        ray.Add(new Swap(this.X, y, this.X, y));
      }
      rays.Add(ray);

      // east
      ray = new List<Swap>();
      x = this.X + 1;
      while (x <= 8 && state.Board[x - 1, this.Y - 1] == 0)
      {
        ray.Add(new Swap(x, this.Y, 0, 0));
        x++;
      }
      if (x <= 8 && Helper.CanCapture(this.Color,
                                      state.Board[x - 1, this.Y - 1]))
      {
        ray.Add(new Swap(x, this.Y, x, this.Y));
      }
      rays.Add(ray);

      // south
      ray = new List<Swap>();
      y = this.Y - 1;
      while (y >= 1 && state.Board[this.X - 1, y - 1] == 0)
      {
        ray.Add(new Swap(this.X, y, 0, 0));
        y--;
      }
      if (y >= 1 && Helper.CanCapture(this.Color,
                                      state.Board[this.X - 1, y - 1]))
      {
        ray.Add(new Swap(this.X, y, this.X, y));
      }
      rays.Add(ray);

      // west
      ray = new List<Swap>();
      x = this.X - 1;
      while (x >= 1 && state.Board[x - 1, this.Y - 1] == 0)
      {
        ray.Add(new Swap(x, this.Y, 0, 0));
        x--;
      }
      if (x >= 1 && Helper.CanCapture(this.Color,
                                      state.Board[x - 1, this.Y - 1]))
      {
        ray.Add(new Swap(x, this.Y, x, this.Y));
      }
      rays.Add(ray);

      return rays;
    }
  }

  public class Knight : Piece
  {
    public Knight(Color color, int x, int y) : base("N", color, x, y) {}

    public override List<List<Swap>>
    Rays(State state, string unused)
    {
      var rays = new List<List<Swap>>();

      // {north,south}{west,east}
      foreach (var y in new int[] { this.Y + 2, this.Y - 2})
      {
        if (y >= 1 && y <= 8)
        {
          foreach (var x in new int[] { this.X + 1, this.X - 1})
          {
            if (x >= 1 && x <= 8)
            {
              if (state.Board[x - 1, y - 1] == 0)
              {
                rays.Add(new List <Swap>() { new Swap(x, y, 0, 0) });
              }
              if (Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
              {
                rays.Add(new List <Swap>() { new Swap(x, y, x, y) });
              }
            }
          }
        }
      }

      // {west,east}{north,south}
      foreach (var x in new int[] { this.X + 2, this.X - 2})
      {
        if (x >= 1 && x <= 8)
        {
          foreach (var y in new int[] { this.Y + 1, this.Y - 1})
          {
            if (y >= 1 && y <= 8)
            {
              if (state.Board[x - 1, y - 1] == 0)
              {
                rays.Add(new List <Swap>() { new Swap(x, y, 0, 0) });
              }
              if (Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
              {
                rays.Add(new List <Swap>() { new Swap(x, y, x, y) });
              }
            }
          }
        }
      }

      return rays;
    }
  }

  public class Bishop : Piece
  {
    public Bishop(Color color, int x, int y) : base("B", color, x, y) {}

    public override List<List<Swap>>
    Rays(State state, string unused)
    {
      var rays = new List<List<Swap>>();
      int x;
      int y;
      List<Swap> ray;

      // northeast
      ray = new List<Swap>();
      x = this.X + 1;
      y = this.Y + 1;
      while (x <= 8 && y <= 8 && state.Board[x - 1, y - 1] == 0)
      {
        ray.Add(new Swap(x, y, 0, 0));
        x++;
        y++;
      }
      if (x <= 8 && y <= 8 &&
          Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
      {
        ray.Add(new Swap(x, y, x, y));
      }
      rays.Add(ray);

      // southeast
      ray = new List<Swap>();
      x = this.X + 1;
      y = this.Y - 1;
      while (x <= 8 && y >= 1 && state.Board[x - 1, y - 1] == 0)
      {
        ray.Add(new Swap(x, y, 0, 0));
        x++;
        y--;
      }
      if (x <= 8 && y >= 1 &&
          Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
      {
        ray.Add(new Swap(x, y, x, y));
      }
      rays.Add(ray);

      // southwest
      ray = new List<Swap>();
      x = this.X - 1;
      y = this.Y - 1;
      while (x >= 1 && y >= 1 && state.Board[x - 1, y - 1] == 0)
      {
        ray.Add(new Swap(x, y, 0, 0));
        x--;
        y--;
      }
      if (x >= 1 && y >= 1 &&
          Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
      {
        ray.Add(new Swap(x, y, x, y));
      }
      rays.Add(ray);

      // northwest
      ray = new List<Swap>();
      x = this.X - 1;
      y = this.Y + 1;
      while (x >= 1 && y <= 8 && state.Board[x - 1, y - 1] == 0)
      {
        ray.Add(new Swap(x, y, 0, 0));
        x--;
        y++;
      }
      if (x >= 1 && y <= 8 &&
          Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
      {
        ray.Add(new Swap(x, y, x, y));
      }
      rays.Add(ray);

      return rays;
    }
  }

  public class Queen : Piece
  {
    public Queen(Color color, int x, int y) : base("Q", color, x, y) {}

    public override List<List<Swap>>
    Rays(State state, string unused)
    {
      var rays = new List<List<Swap>>();
      rays.AddRange((new Rook(this.Color, this.X, this.Y)).Rays(state, null));
      rays.AddRange((new Bishop(this.Color, this.X, this.Y)).Rays(state, null));
      return rays;
    }
  }

  public class King : Piece
  {
    public King(Color color, int x, int y) : base("K", color, x, y) {}

    public override List<List<Swap>>
    Rays(State state, string unsed)
    {
      var rays = new List<List<Swap>>();

      foreach (var y in new[] { this.Y - 1, this.Y, this.Y + 1 })
      {
        if (y < 1 || y > 8)
        {
          continue;
        }
        foreach (var x in new[] { this.X - 1, this.X, this.X + 1 })
        {
          if (x < 1 || x > 8)
          {
            continue;
          }
          if (x == this.X && y == this.Y)
          {
            continue;
          }
          if (state.Board[x - 1, y - 1] == 0)
          {
            rays.Add(new List<Swap>() { new Swap(x, y, 0, 0) });
          }
          if (Helper.CanCapture(this.Color, state.Board[x - 1, y - 1]))
          {
            rays.Add(new List<Swap>() { new Swap(x, y, x, y) });
          }
        }
      }

      int xMore = 0;
      int yMore = 0;
      foreach (var x in new[] { 3, 7 })
      {
        if (this.Castle(state, x, this.Y, out xMore, out yMore)
            .Contains(Ret.Castle))
        {
          rays.Add(new List<Swap>() { new Swap(x, this.Y, xMore, yMore) });
        }
      }

      return rays;
    }


    // Castle: Determine possibility of castling moves.

    public override List<Ret>
    Castle(State state, int x, int y, out int xMore, out int yMore)
    {
      xMore = 0;
      yMore = 0;
      var bad = new List<Ret>() { Ret.BadCastle };
      if (! this.Inert)
      {
        return bad;
      }
      if (this.Color == Color.Black && ! ((x == 3 || x == 7) && y == 8))
      {
        return bad;
      }
      if (this.Color == Color.White && ! ((x == 3 || x == 7) && y == 1))
      {
        return bad;
      }
      int xRook = 8;
      if (x < 5) // queenside
      {
        xRook = 1;
      }
      var rook = state.At(state.Live, xRook, y);
      if (rook == null ||
          ! (Is(rook, "Rook") && rook.Color == this.Color && rook.Inert))
      {
        return bad;
      }

      // get ray from king to rook, inclusive
      var ray = new List<Swap>();
      var range = new[] { 6, 7 };
      if (x < 5) // queenside
      {
        range = new[] { 4, 3, 2 };
      }
      ray.Add(new Swap(this.X, y, this.X, y));
      foreach (var file in range)
      {
        if (state.At(state.Live, file, y) != null)
        {
          return bad;
        }
        ray.Add(new Swap(file, y, 0, 0));
      }
      ray.Add(new Swap(rook.X, y, rook.X, y));

      // is the castling threatened?
      string last = state.LastNote();
      foreach (var swap in ray)
      {
        foreach (var piece in state.Live)
        {
          if (piece.Color == this.Color)
          {
            continue;
          }
          foreach (var reach in piece.Reach(state, last))
          {
            if (reach == swap)
            {
              return bad;
            }
          }
        }
      }

      xMore = rook.X;
      yMore = y;
      return new List<Ret>() { Ret.Castle };
    }
  }
}
