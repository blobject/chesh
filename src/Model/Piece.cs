using System;
using System.Collections.Generic;
using Chesh.Util;

namespace Chesh.Model
{

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

    public abstract List<List<(int,int,int,int)>>
    Rays(char[,] board, string last);


    // Reach: Flattened Rays.

    public virtual List<(int,int,int,int)>
    Reach(char[,] board, string last)
    {
      var reach = new List<(int,int,int,int)>();
      foreach (var ray in this.Rays(board, last))
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
    public virtual List<(int,int,int,int)>
    EnPassant(char[,] board, string last, int x, int y)
    {
      return null;
    }


    // Move: Move the piece.

    public List<Ret>
    Move(State state, int x, int y, out int xMore, out int yMore)
    {
      xMore = 0;
      yMore = 0;
      var board = state.Board;
      if (Is(this, "King"))
      {
        var rets = this.Castle(state, x, y, out xMore, out yMore);
        if (rets.Contains(Ret.Castle))
        {
          return rets;
        }
      }
      string last = state.LastNote();
      foreach (var swap in this.Reach(board, last))
      {
        if (swap.Item1 == x && swap.Item2 == y)
        {
          xMore = swap.Item3;
          yMore = swap.Item4;
          if (xMore > 0 && yMore > 0 &&
              Helper.CanCapture(this.Color, board[xMore - 1, yMore - 1]))
          {
            if (xMore == x && yMore == y)
            {
              return new List<Ret>() { Ret.Capture };
            }
            return new List<Ret>() { Ret.EnPassant };
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

    public override List<(int,int,int,int)>
    EnPassant(char[,] board, string last, int x, int y)
    {
      if (last != null && last[0] == 'P')
      {
        bool ep = false;
        (int,int,int,int) enemy = Helper.MoveToInts(last.Substring(1));
        if (this.Color == Color.Black)
        {
          if (x == enemy.Item1 && x == enemy.Item3 &&
              2 == enemy.Item2 && 4 == enemy.Item4)
          {
            ep = true;
          }
        }
        else
        {
          if (x == enemy.Item1 && x == enemy.Item3 &&
              7 == enemy.Item2 && 5 == enemy.Item4)
          {
            ep = true;
          }
        }
        if (ep && Helper.CanCapture(this.Color, board[x - 1, enemy.Item4 - 1]))
        {
          return new List<(int,int,int,int)>() { (x, y, x, enemy.Item4) };
        }
      }
      return null;
    }

    public override List<List<(int,int,int,int)>>
    Rays(char[,] board, string last)
    {
      var rays = new List<List<(int,int,int,int)>>();
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
      if (board[this.X - 1, y - 1] == 0)
      {
        rays.Add(new List<(int,int,int,int)>() { (this.X, y, 0, 0) });
      }

      // capture
      List<(int,int,int,int)> epSwap;
      x = this.X - 1;
      if (x >= 1)
      {
        if (Helper.CanCapture(this.Color, board[x - 1, y - 1]))
        {
          rays.Add(new List<(int,int,int,int)>() { (x, y, x, y) });
        }

        // en passant
        epSwap = this.EnPassant(board, last, x, y);
        if (epSwap != null)
        {
          rays.Add(epSwap);
        }
      }
      x = this.X + 1;
      if (x <= 8)
      {
        if (Helper.CanCapture(this.Color, board[x - 1, y - 1]))
        {
          rays.Add(new List<(int,int,int,int)>() { (x, y, x, y) });
        }

        // en passant
        epSwap = this.EnPassant(board, last, x, y);
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
      if (rays.Count > 0 && board[this.X - 1, y - 1] == 0)
      {
        rays.Add(new List<(int,int,int,int)>() { (this.X, y, 0, 0) });
      }

      return rays;
    }
  }

  public class Rook : Piece
  {
    public Rook(Color color, int x, int y) : base("R", color, x, y) {}

    public override List<List<(int,int,int,int)>>
    Rays(char[,] board, string unused)
    {
      var rays = new List<List<(int,int,int,int)>>();
      int x;
      int y;
      List<(int,int,int,int)> ray;

      // north
      ray = new List<(int,int,int,int)>();
      y = this.Y + 1;
      while (y <= 8 && board[this.X - 1, y - 1] == 0)
      {
        ray.Add((this.X, y, 0, 0));
        y++;
      }
      if (y <= 8 && Helper.CanCapture(this.Color, board[this.X - 1, y - 1]))
      {
        ray.Add((this.X, y, this.X, y));
      }
      rays.Add(ray);

      // east
      ray = new List<(int,int,int,int)>();
      x = this.X + 1;
      while (x <= 8 && board[x - 1, this.Y - 1] == 0)
      {
        ray.Add((x, this.Y, 0, 0));
        x++;
      }
      if (x <= 8 && Helper.CanCapture(this.Color, board[x - 1, this.Y - 1]))
      {
        ray.Add((x, this.Y, x, this.Y));
      }
      rays.Add(ray);

      // south
      ray = new List<(int,int,int,int)>();
      y = this.Y - 1;
      while (y >= 1 && board[this.X - 1, y - 1] == 0)
      {
        ray.Add((this.X, y, 0, 0));
        y--;
      }
      if (y >= 1 && Helper.CanCapture(this.Color, board[this.X - 1, y - 1]))
      {
        ray.Add((this.X, y, this.X, y));
      }
      rays.Add(ray);

      // west
      ray = new List<(int,int,int,int)>();
      x = this.X - 1;
      while (x >= 1 && board[x - 1, this.Y - 1] == 0)
      {
        ray.Add((x, this.Y, 0, 0));
        x--;
      }
      if (x >= 1 && Helper.CanCapture(this.Color, board[x - 1, this.Y - 1]))
      {
        ray.Add((x, this.Y, x, this.Y));
      }
      rays.Add(ray);

      return rays;
    }
  }

  public class Knight : Piece
  {
    public Knight(Color color, int x, int y) : base("N", color, x, y) {}

    public override List<List<(int,int,int,int)>>
    Rays(char[,] board, string unused)
    {
      var rays = new List<List<(int,int,int,int)>>();

      // {north,south}{west,east}
      foreach (var y in new int[] { this.Y + 2, this.Y - 2})
      {
        if (y >= 1 && y <= 8)
        {
          foreach (var x in new int[] { this.X + 1, this.X - 1})
          {
            if (x >= 1 && x <= 8)
            {
              if (board[x - 1, y - 1] == 0)
              {
                rays.Add(new List <(int,int,int,int)>() { (x, y, 0, 0) });
              }
              if (Helper.CanCapture(this.Color, board[x - 1, y - 1]))
              {
                rays.Add(new List <(int,int,int,int)>() { (x, y, x, y) });
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
              if (board[x - 1, y - 1] == 0)
              {
                rays.Add(new List <(int,int,int,int)>() { (x, y, 0, 0) });
              }
              if (Helper.CanCapture(this.Color, board[x - 1, y - 1]))
              {
                rays.Add(new List <(int,int,int,int)>() { (x, y, x, y) });
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

    public override List<List<(int,int,int,int)>>
    Rays(char[,] board, string unused)
    {
      var rays = new List<List<(int,int,int,int)>>();
      int x;
      int y;
      List<(int,int,int,int)> ray;

      // northeast
      ray = new List<(int,int,int,int)>();
      x = this.X + 1;
      y = this.Y + 1;
      while (x <= 8 && y <= 8 && board[x - 1, y - 1] == 0)
      {
        ray.Add((x, y, 0, 0));
        x++;
        y++;
      }
      if (x <= 8 && y <= 8 &&
          Helper.CanCapture(this.Color, board[x - 1, y - 1]))
      {
        ray.Add((x, y, x, y));
      }
      rays.Add(ray);

      // southeast
      ray = new List<(int,int,int,int)>();
      x = this.X + 1;
      y = this.Y - 1;
      while (x <= 8 && y >= 1 && board[x - 1, y - 1] == 0)
      {
        ray.Add((x, y, 0, 0));
        x++;
        y--;
      }
      if (x <= 8 && y >= 1 &&
          Helper.CanCapture(this.Color, board[x - 1, y - 1]))
      {
        ray.Add((x, y, x, y));
      }
      rays.Add(ray);

      // southwest
      ray = new List<(int,int,int,int)>();
      x = this.X - 1;
      y = this.Y - 1;
      while (x >= 1 && y >= 1 && board[x - 1, y - 1] == 0)
      {
        ray.Add((x, y, 0, 0));
        x--;
        y--;
      }
      if (x >= 1 && y >= 1 &&
          Helper.CanCapture(this.Color, board[x - 1, y - 1]))
      {
        ray.Add((x, y, x, y));
      }
      rays.Add(ray);

      // northwest
      ray = new List<(int,int,int,int)>();
      x = this.X - 1;
      y = this.Y + 1;
      while (x >= 1 && y <= 8 && board[x - 1, y - 1] == 0)
      {
        ray.Add((x, y, 0, 0));
        x--;
        y++;
      }
      if (x >= 1 && y <= 8 &&
          Helper.CanCapture(this.Color, board[x - 1, y - 1]))
      {
        ray.Add((x, y, x, y));
      }
      rays.Add(ray);

      return rays;
    }
  }

  public class Queen : Piece
  {
    public Queen(Color color, int x, int y) : base("Q", color, x, y) {}

    public override List<List<(int,int,int,int)>>
    Rays(char[,] board, string unused)
    {
      var rays = new List<List<(int,int,int,int)>>();
      rays.AddRange((new Rook(this.Color, this.X, this.Y)).Rays(board, null));
      rays.AddRange((new Bishop(this.Color, this.X, this.Y)).Rays(board, null));
      return rays;
    }
  }

  public class King : Piece
  {
    public King(Color color, int x, int y) : base("K", color, x, y) {}

    public override List<List<(int,int,int,int)>>
    Rays(char[,] board, string unsed)
    {
      var rays = new List<List<(int,int,int,int)>>();

      foreach (var y in new[] { this.Y - 1, this.Y, this.Y + 1})
      {
        if (y < 1 || y > 8)
        {
          continue;
        }
        foreach (var x in new[] { this.X - 1, this.X, this.X + 1})
        {
          if (x < 1 || x > 8)
          {
            continue;
          }
          if (x == this.X && y == this.Y)
          {
            continue;
          }
          if (board[x - 1, y - 1] == 0)
          {
            rays.Add(new List<(int,int,int,int)>() { (x, y, 0, 0) });
          }
          if (Helper.CanCapture(this.Color, board[x - 1, y - 1]))
          {
            rays.Add(new List<(int,int,int,int)>() { (x, y, x, y) });
          }
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
      if (this.Color == Color.Black && ((x != 3 && x != 7) || y != 8))
      {
        return bad;
      }
      if (this.Color == Color.White && ((x != 3 && x != 7) || y != 1))
      {
        return bad;
      }
      int xRook = 8;
      if (x < 5) // queenside
      {
        xRook = 1;
      }
      var rook = state.At(state.Live, xRook, y);
      if (rook == null || ! Is(rook, "Rook") || ! rook.Inert)
      {
        return bad;
      }

      // get ray from king to rook, inclusive
      var ray = new List<(int,int,int,int)>();
      var range = new[] { 6, 7 };
      if (x < 5) // queenside
      {
        range = new[] { 4, 3, 2 };
      }
      ray.Add((this.X, y , this.X, y));
      foreach (var file in range)
      {
        if (state.At(state.Live, file, y) != null)
        {
          return bad;
        }
        ray.Add((file, y, 0, 0));
      }
      ray.Add((rook.X, y, rook.X, y));

      // is the castling threatened?
      Color turn = state.Turn(true);
      string last = state.LastNote();
      var board = state.Board;
      foreach (var swap in ray)
      {
        foreach (var piece in state.Live)
        {
          if (piece.Color == turn)
          {
            continue;
          }
          foreach (var reach in piece.Reach(board, last))
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
