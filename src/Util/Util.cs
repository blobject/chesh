using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Chesh.Model;

namespace Chesh.Util
{

  // Helper: Various static helper methods used by other objects.

  public class Helper
  {

    // JsonIsNull: Check whether a json object is null.

    public static bool
    JsonIsNull(string json, string key)
    {
      return ((JsonElement) FromJson(json)).GetProperty(key).ValueKind
        == JsonValueKind.Null;
    }


    // FromJson: Convert a json object to string.

    public static object
    FromJson(string s)
    {
      return JsonSerializer.Deserialize<object>(s);
    }


    // JsonToValue: Get a key's value from a json object.

    public static string
    JsonToValue(string json, string key)
    {
      return ((JsonElement) FromJson(json)).GetProperty(key).GetString();
    }


    // JsonToSelection: Get State.Selection from a json object.

    public static JsonElement
    JsonToSelection(string state)
    {
      return ((JsonElement) FromJson(state)).GetProperty("Selection");
    }

    // JsonToStateList: Get a list value from a json object.

    public static IEnumerable<JsonElement>
    JsonToStateList(string state, string key)
    {
      return ((JsonElement) FromJson(state)).GetProperty(key).EnumerateArray();
    }


    // LastDead: Get the last dead piece from a json object.

    public static JsonElement
    LastDead(string state)
    {
      JsonElement piece = new JsonElement();
      foreach (var dead in JsonToStateList(state, "Dead"))
      {
        piece = dead;
      }
      return piece;
    }


    // ToJson: Convert an object to a serialised json string.

    public static string
    ToJson(object o)
    {
      var options = new JsonSerializerOptions();
      options.Converters.Add(new HistoryConverter());
      options.Converters.Add(new PieceConverter());
      options.Converters.Add(new SwapConverter());
      return JsonSerializer.Serialize(o, options);
    }


    // ToFileNum: Get the numeric value of a file.

    public static int
    ToFileNum(char file)
    {
      return (int) file - 'a' + 1;
    }


    // ToRankNum: Get the numeric value of a rank.

    public static int
    ToRankNum(char rank)
    {
      return (int) char.GetNumericValue(rank);
    }


    // ToFileChar: Get the alphabetic value of a file.

    public static char
    ToFileChar(int file)
    {
      return (char) (file + 'a' - 1);
    }


    // ToRankChar: Get the numeric character of a rank.

    public static char
    ToRankChar(int rank)
    {
      return (char) (rank + '1' - 1);
    }


    // SymToName: Translate piece sym to its name.

    public static string
    SymToName(string sym)
    {
      if (sym == null)
      {
        return null;
      }
      switch (sym.ToUpper())
      {
        case "P": return "Pawn";
        case "R": return "Rook";
        case "N": return "Knight";
        case "B": return "Bishop";
        case "Q": return "Queen";
        case "K": return "King";
        default: return null;
      }
    }


    // IntsToSquare: Convert numeric coordinates to its square string.

    public static string
    IntsToSquare(int x, int y)
    {
      return $"{ToFileChar(x)}{ToRankChar(y)}";
    }


    // MoveToSwap: Convert a move notation to a quadruple of numbers.

    public static Swap
    MoveToSwap(string move)
    {
      return new Swap(ToFileNum(move[0]), ToRankNum(move[1]),
                      ToFileNum(move[2]), ToRankNum(move[3]));
    }


    // Notate: Create a notation from movement data.

    public static string
    Notate(List<Ret> rets,
           string sym, char prom,
           int xSrc, int ySrc,
           int xDst, int yDst)
    {
      char fileSrc = Helper.ToFileChar(xSrc);
      char fileDst = Helper.ToFileChar(xDst);
      string note = $"{sym.ToUpper()}{fileSrc}{ySrc}{fileDst}{yDst}";
      string suffix = string.Empty;
      if (rets.Count == 0)
      {
        return note;
      }
      if (rets.Contains(Ret.Promote))
      {
        suffix += prom.ToString();
      }
      if (rets.Contains(Ret.Castle))
      {
        suffix += "%";
      }
      if (rets.Contains(Ret.EnPassant))
      {
        suffix += "p";
      }
      if (rets.Contains(Ret.Capture))
      {
        suffix += ":";
      }
      if (rets.Contains(Ret.Check))
      {
        suffix += "+";
      }
      if (rets.Contains(Ret.Checkmate))
      {
        suffix += "#";
      }
      if (suffix.Contains("p:"))
      {
        suffix = Regex.Replace(suffix, @"p:", "p");
      }
      if (suffix.Contains(":+"))
      {
        suffix = Regex.Replace(suffix, @":\+", "*");
      }
      if (suffix.Contains(":#"))
      {
        suffix = Regex.Replace(suffix, @":#", "&");
      }
      return note + suffix;
    }


    // Denotate: Extract just the move from a notation.

    public static string
    Denotate(string note)
    {
      if (note == "bye")
      {
        return note;
      }
      if (note == "tie")
      {
        return note;
      }
      return Regex.Replace(note.Trim().Substring(1),
                           @"[*&#+:p%RNBQ]$", string.Empty);
    }


    // ValidSquare: Determine whether the square is valid.

    public static bool
    ValidSquare(char file, char rank)
    {
      int f = ToFileNum(char.ToLower(file));
      int r = ToRankNum(char.ToLower(rank));
      if (f < 1 || f > 8 || r < 1 || r > 8)
      {
        return false;
      }
      return true;
    }


    // CanCapture: Determine whether an abbreviation can be captured.
    //             State.Board consists only of abbreviations.

    public static bool
    CanCapture(Color color, char that)
    {
      if (color == Color.Black && char.IsUpper(that))
      {
        return true;
      }
      if (color == Color.White && char.IsLower(that))
      {
        return true;
      }
      return false;
    }


    // StrandsToPieces: Convert a string in shorthand notation to pieces.
    //                      Used for testing purposes.
    // shorthand notation: <color><sym><file><rank>, ex: bq41

    public static List<Piece>
    StrandsToPieces(string strands)
    {
      var pieces = new List<Piece>();
      Color color;
      string sym;
      char f;
      char r;
      int file;
      int rank;
      Piece piece;
      foreach (var strand in strands.ToLower().Split())
      {
        if (strand.Length != 4)
        {
          continue;
        }

        // color
        if (char.ToLower(strand[0]) == 'b')
        {
          color = Color.Black;
        }
        else if (char.ToLower(strand[0]) == 'w')
        {
          color = Color.White;
        }
        else
        {
          continue;
        }

        // sym
        sym = char.ToLower(strand[1]).ToString();
        if (! Regex.IsMatch(sym, @"[prnbqk]"))
        {
          continue;
        }

        // square
        f = strand[2];
        r = strand[3];
        if (! ValidSquare(f, r))
        {
          continue;
        }
        file = ToFileNum(char.ToLower(f));
        rank = ToRankNum(char.ToLower(r));

        // piece
        piece = new Pawn(color, file, rank);
        if (sym == "r")
        {
          piece = new Rook(color, file, rank);
        }
        else if (sym == "n")
        {
          piece = new Knight(color, file, rank);
        }
        else if (sym == "b")
        {
          piece = new Bishop(color, file, rank);
        }
        else if (sym == "q")
        {
          piece = new Queen(color, file, rank);
        }
        else if (sym == "k")
        {
          piece = new King(color, file, rank);
        }

        pieces.Add(piece);
      }
      return pieces;
    }
  }


  // ISubject: Observable interface.

  public interface ISubject
  {
    void Attach(IObserver observer);
    void StateChanged(State next);
    void CfgChanged(Dictionary<string,string> next);
  }


  // IObserver: Observer interface.
  public interface IObserver
  {
    void ChangeState(State next);
    void ChangeCfg(Dictionary<string,string> next);
  }


  // HistoryConverter: Know how to convert the History object into json.

  public class HistoryConverter : JsonConverter<(string,long)>
  {
    public override (string,long)
    Read(ref Utf8JsonReader reader,
         Type typeToConvert,
         JsonSerializerOptions options)
    {
      // stub
      return (null, 0);
    }

    public override void
    Write(Utf8JsonWriter writer,
          (string,long) note,
          JsonSerializerOptions options)
    {
      writer.WriteStartArray();
      writer.WriteStringValue(note.Item1);
      writer.WriteNumberValue(note.Item2);
      writer.WriteEndArray();
    }
  }


  // PieceConverter: Know how to convert a Piece object into json.

  public class PieceConverter : JsonConverter<Piece>
  {
    public override Piece
    Read(ref Utf8JsonReader reader,
         Type typeToConvert,
         JsonSerializerOptions options)
    {
      // stub
      return null;
    }

    public override void
    Write(Utf8JsonWriter writer,
          Piece piece,
          JsonSerializerOptions options)
    {
      bool color = false;
      if (piece.Color == Color.Black)
      {
        color = true;
      }
      writer.WriteStartArray();
      writer.WriteStringValue(piece.Sym);
      writer.WriteBooleanValue(color);
      writer.WriteNumberValue(piece.X);
      writer.WriteNumberValue(piece.Y);
      writer.WriteBooleanValue(piece.Inert);
      writer.WriteEndArray();
    }
  }


  // SwapConverter: Know how to convert the Swap object into json.

  public class SwapConverter : JsonConverter<Swap>
  {
    public override Swap
    Read(ref Utf8JsonReader reader,
         Type typeToConvert,
         JsonSerializerOptions options)
    {
      // stub
      return null;
    }

    public override void
    Write(Utf8JsonWriter writer,
          Swap swap,
          JsonSerializerOptions options)
    {
      writer.WriteStartArray();
      writer.WriteNumberValue(swap.X);
      writer.WriteNumberValue(swap.Y);
      writer.WriteNumberValue(swap.XMore);
      writer.WriteNumberValue(swap.YMore);
      writer.WriteEndArray();
    }
  }
}
