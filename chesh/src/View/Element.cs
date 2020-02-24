using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Chesh.Util;

namespace Chesh.View
{

  // Element: A rectangular Ui element.

  public abstract class Element
  {
    public string Style;
    public int X;
    public int Y;
    public int Width;
    public int Height;


    // Erase: Fill the space occupied by the element with spaces.

    public void
    Erase()
    {
      int width = this.Width;
      if ((new List<string>() {
            "BlackResponseElement",
            "WhiteResponseElement",
            "BlackPromptElement",
            "WhitePromptElement"
          }).Contains(this.GetType().Name))
      {
        width = Ui.HostWidth;
      }
      for (int i = 0; i < this.Height; i++)
      {
        Ui.Pin(this.X, this.Y + i);
        Ui.Write(new string(' ', width));
      }
      Ui.Pin(this.X, this.Y);
    }


    // Draw: Display the element.

    public abstract void Draw(string data);


    // SetCfg: Set the cfg of the element.

    public abstract void SetCfg(string cfg);


    // stubs: implemented only by Menu

    public virtual void Nav(bool up, string cfg) {}

    public virtual Option Enter() { return Option.Resume; }

    public virtual void Entry(Option opt, string cfg) {}
  }


  // Option: Menu items.

  public enum Option { None, Resume, Undo, Reset, Quit, Style }


  // Menu: Control for meta/game-level-actions players can take.

  public class Menu : Element
  {
    private List<(Option,int)> Options;
    private (Option,int) Selected;
    public int InnerWidth;
    public int InnerHeight;

    public Menu(string style)
    {
      this.Style = style;
      this.X = 0;
      this.Y = 0;
      this.Width = Ui.Width;
      this.Height = Ui.Height;
      this.Options = new List<(Option,int)>()
        {
          (Option.Resume, 3),
          (Option.Undo, 5),
          (Option.Reset, 7),
          (Option.Quit, 9),
          (Option.Style, 11)
        };
      this.Selected = this.Options[0];
      this.InnerWidth = this.Width - 2;
      this.InnerHeight = 2 * this.Options.Count + 3;
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      this.Width = Ui.Width;
      this.Height = Ui.Height;
      this.InnerWidth = this.Width - 2;
      this.InnerHeight = 2 * this.Options.Count + 3;
    }

    public override void
    Draw(string cfg)
    {
      Ui.Pin(this.X + 1, this.Y + 1);
      Ui.Write("." + new string('-', this.InnerWidth) + ".");
      for (int i = 2; i < this.InnerHeight; i++)
      {
        Ui.Pin(this.X + 1, this.Y + i);
        Ui.Write("|" + new string(' ', this.InnerWidth) + "|");
      }
      Ui.Pin(this.X + 1, this.InnerHeight);
      Ui.Write("'" + new string('-', this.InnerWidth) + "'");

      foreach (var opt in this.Options)
      {
        Ui.Pin(this.X + 4, this.Y + opt.Item2);
        this.Entry(opt.Item1, cfg);
      }

      Ui.Pin(this.X + 3, this.Y + this.Selected.Item2);
    }


    // Entry: An item in the menu.

    public override void
    Entry(Option opt, string cfg)
    {
      string text = string.Empty;
      string more = string.Empty;
      if (opt == Option.Undo)
      {
        text = "Undo move";
      }
      else if (opt == Option.Reset)
      {
        text = "Reset game";
      }
      else if (opt == Option.Quit)
      {
        text = "Quit game";
      }
      else if (opt == Option.Style)
      {
        text = "style";
        more = "[" + Helper.JsonToCfgValue(cfg, "style") + "]";
      }
      else
      {
        text = "Resume game";
      }
      string entry = $"  {text}   {more}";
      if (opt == this.Selected.Item1)
      {
        entry = $"> {text} < {more}";
      }
      Ui.Write(entry);
    }


    // Nav: Change selection of menu items.

    public override void
    Nav(bool up, string cfg)
    {
      int i = this.Options.IndexOf(this.Selected);
      if (up)
      {
        i--;
        if (i < 0)
        {
          i = this.Options.Count - 1;
        }
      }
      else
      {
        i++;
        if (i > this.Options.Count - 1)
        {
          i = 0;
        }
      }
      var old = this.Selected;
      this.Selected = this.Options[i];
      Ui.Pin(this.X + 4, this.Y + old.Item2);
      this.Entry(old.Item1, cfg);
      Ui.Pin(this.X + 4, this.Y + this.Selected.Item2);
      this.Entry(this.Selected.Item1, cfg);
      Ui.Pin(this.X + 3, this.Y + (this.Selected.Item2));
    }


    // Enter: Called when a menu item is chosen.

    public override Option
    Enter()
    {
      return this.Selected.Item1;
    }
  }


  // ResponseElement: Displays additional messages to players.

  public abstract class ResponseElement : Element
  {
    public ResponseElement(string style)
    {
      this.Style = style;
      this.Height = 1;
    }

    public override abstract void SetCfg(string cfg);

    public override void
    Draw(string message)
    {
      if (message == null)
      {
        return;
      }
      Ui.Pin(this.X, this.Y);
      Ui.Write(message);
    }
  }


  public class MenuResponseElement : ResponseElement
  {
    public MenuResponseElement(string style) : base(style)
    {
      this.Y = 18;
      switch (style)
      {
        case "compact":
          this.X = 1;
          break;
        case "wide":
          this.X = 2;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 1;
          break;
        case "wide":
          this.X = 2;
          break;
      }
    }
  }

  public class BlackResponseElement : ResponseElement
  {
    public BlackResponseElement(string style) : base(style)
    {
      this.Style = style;
      switch (style)
      {
        case "compact":
          this.X = 0;
          this.Y = 0;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 1;
          this.Width = 48;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 0;
          this.Y = 0;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 1;
          this.Width = 48;
          break;
      }
    }
  }

  public class WhiteResponseElement : ResponseElement
  {
    public WhiteResponseElement(string style) : base(style)
    {
      this.Style = style;
      switch (style)
      {
        case "compact":
          this.X = 0;
          this.Y = 19;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 29;
          this.Width = 48;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 0;
          this.Y = 19;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 29;
          this.Width = 48;
          break;
      }
    }
  }


  // PromptElement: Where players input their commands.
  //                Also used sometimes to display responses.

  public abstract class PromptElement : Element
  {
    public PromptElement(string cfg)
    {
      this.Height = 1;
    }

    public override abstract void Draw(string message);
  }

  public class BlackPromptElement : PromptElement
  {
    public BlackPromptElement(string style) : base(style)
    {
      this.Style = style;
      switch (style)
      {
        case "compact":
          this.X = 0;
          this.Y = 1;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 2;
          this.Width = 58;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 0;
          this.Y = 1;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 2;
          this.Width = 58;
          break;
      }
    }

    public override void
    Draw(string message)
    {
      Ui.Pin(this.X, this.Y);
      Ui.Write("b> ");
      if (message != null)
      {
        Ui.Write(message);
      }
    }
  }

  public class WhitePromptElement : PromptElement
  {
    public WhitePromptElement(string style) : base(style)
    {
      this.Style = style;
      switch (style)
      {
        case "compact":
          this.X = 0;
          this.Y = 18;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 28;
          this.Width = 58;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 0;
          this.Y = 18;
          this.Width = 26;
          break;
        case "wide":
          this.X = 1;
          this.Y = 28;
          this.Width = 58;
          break;
      }
    }

    public override void
    Draw(string message)
    {
      Ui.Pin(this.X, this.Y);
      Ui.Write("w> ");
      if (message != null)
      {
        Ui.Write(message);
      }
    }
  }


  // DeadElement: Displays list of dead pieces

  public abstract class DeadElement : Element
  {
    public DeadElement(string style)
    {
      this.Style = style;
    }

    public override abstract void Draw(string state);
  }

  public class WhiteDeadElement : DeadElement
  {
    public WhiteDeadElement(string style) : base(style)
    {
      switch (style)
      {
        case "compact":
          this.X = 14;
          this.Y = 3;
          this.Width = 10;
          this.Height = 2;
          break;
        case "wide":
          this.X = 21;
          this.Y = 4;
          this.Width = 35;
          this.Height = 1;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 14;
          this.Y = 3;
          this.Width = 10;
          this.Height = 2;
          break;
        case "wide":
          this.X = 21;
          this.Y = 4;
          this.Width = 35;
          this.Height = 1;
          break;
      }
    }

    public override void
    Draw(string state)
    {
      Ui.Pin(this.X, this.Y);
      Ui.Write("x");
      Ui.Pin(this.X + this.Width + 1, this.Y);
      Ui.Write("x");
      Ui.Pin(this.X + 2, this.Y);
      int count = 0;
      foreach (JsonElement piece in
               Helper.JsonToStateListValue(state, "Dead"))
      {
        if (! piece[1].GetBoolean()) // white
        {
          if (this.Style == "compact")
          {
            if (count >= 8)
            {
              Ui.Pin(this.X + 17 - count, this.Y - 1);
            }
            Ui.Write(piece[0].GetString()); // sym
            count++;
          }
          else if (this.Style == "wide")
          {
            Ui.Write(piece[0].GetString() + " "); // sym
          }
        }
      }
    }
  }

  public class BlackDeadElement : DeadElement
  {
    public BlackDeadElement(string style) : base(style)
    {
      switch (style)
      {
        case "compact":
          this.X = 14;
          this.Y = 16;
          this.Width = 10;
          this.Height = 2;
          break;
        case "wide":
          this.X = 21;
          this.Y = 26;
          this.Width = 35;
          this.Height = 1;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 14;
          this.Y = 16;
          this.Width = 10;
          this.Height = 2;
          break;
        case "wide":
          this.X = 21;
          this.Y = 26;
          this.Width = 35;
          this.Height = 1;
          break;
      }
    }

    public override void
    Draw(string state)
    {
      Ui.Pin(this.X, this.Y);
      Ui.Write("x");
      Ui.Pin(this.X + this.Width + 1, this.Y);
      Ui.Write("x");
      Ui.Pin(this.X + 2, this.Y);
      int count = 0;
      string name;
      foreach (JsonElement piece in
               Helper.JsonToStateListValue(state, "Dead"))
      {
        if (piece[1].GetBoolean()) // black
        {
          name = piece[0].GetString();
          if (name == "p")
          {
            // pawn case disambiguation
            name = "o";
          }
          if (this.Style == "compact")
          {
            if (count >= 8)
            {
              Ui.Pin(this.X + 17 - count, this.Y + 1);
            }
            Ui.Write(name); // sym
            count++;
          }
          else if (this.Style == "wide")
          {
            Ui.Write(name + " "); // sym
          }
        }
      }
    }
  }


  // BoardFrameElement: Displays the chess board.

  public class BoardFrameElement : Element
  {
    public BoardFrameElement(string style)
    {
      this.Style = style;
      switch (style)
      {
        case "compact":
          this.X = 14;
          this.Y = 4;
          this.Width = 12;
          this.Height = 12;
          break;
        case "wide":
          this.X = 21;
          this.Y = 6;
          this.Width = 37;
          this.Height = 19;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 14;
          this.Y = 4;
          this.Width = 12;
          this.Height = 12;
          break;
        case "wide":
          this.X = 21;
          this.Y = 6;
          this.Width = 37;
          this.Height = 19;
          break;
      }
    }

    public override void
    Draw(string state)
    {
      if (this.Style == "compact")
      {
        Ui.Pin(this.X, this.Y);
        Ui.Write("  ");
        for (int file = 1; file <= 8; file++)
        {
          Ui.Write(Helper.ToFileChar(file).ToString());
        }
        Ui.Pin(this.X, this.Y + 1);
        Ui.Write(" ." + new string('-', 8) + ".");
        Ui.Pin(this.X, this.Y + 2);
        for (int rank = 8; rank >= 1; rank--)
        {
          Ui.Write(rank + "|" + new string(' ', 8) + "|" + rank);
          Ui.Pin(this.X, this.Y + this.Height - 1 - rank);
        }
        Ui.Pin(this.X, this.Y + this.Width - 2);
        Ui.Write(" '" + new string('-', 8) + "'");
        Ui.Pin(this.X, this.Y + this.Width - 1);
        Ui.Write("  ");
        for (int file = 1; file <= 8; file++)
        {
          Ui.Write(Helper.ToFileChar(file).ToString());
        }
      }
      else if (this.Style == "wide")
      {
        int y = this.Y;
        Ui.Pin(this.X + 1, y);
        for (int file = 1; file <= 8; file++)
        {
          Ui.Write($"   {Helper.ToFileChar(file)}");
        }
        Ui.Pin(this.X, ++y);
        Ui.Write("  .");
        for (int file = 1; file <= 8; file++)
        {
          Ui.Write("---.");
        }
        for (int rank = 8; rank >= 1; rank--)
        {
          Ui.Pin(this.X, ++y);
          Ui.Write($"{rank} |");
          for (int file = 1; file <= 8; file++)
          {
            Ui.Write("   |");
          }
          Ui.Write($" {rank}");
          Ui.Pin(this.X, ++y);
          if (rank > 1)
          {
            Ui.Write("  |");
            for (int file = 1; file <= 8; file++)
            {
              if (file < 8)
              {
                Ui.Write("---+");
              }
              else
              {
                Ui.Write("---|");
              }
            }
          }
          else
          {
            Ui.Write("  '");
            for (int file = 1; file <= 8; file++)
            {
              Ui.Write("---'");
            }
          }
        }
        Ui.Pin(this.X + 1, ++y);
        for (int file = 1; file <= 8; file++)
        {
          Ui.Write($"   {Helper.ToFileChar(file)}");
        }
      }
    }
  }


  // HistoryElement: Displays the log of moves.

  public class HistoryElement : Element
  {
    public HistoryElement(string style)
    {
      this.Style = style;
      this.X = 0;
      switch (style)
      {
        case "compact":
          this.Y = 2;
          this.Width = 13;
          this.Height = 16;
          break;
        case "wide":
          this.Y = 4;
          this.Width = 20;
          this.Height = 23;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.Y = 2;
          this.Width = 13;
          this.Height = 16;
          break;
        case "wide":
          this.Y = 4;
          this.Width = 20;
          this.Height = 39;
          break;
      }
    }

    public override void
    Draw(string state)
    {
      var history = Helper.JsonToStateListValue(state, "History");
      if (history == null)
      {
        return;
      }
      var rev = new Stack<(string,int)>();
      var stack = new Stack<(string,int)>();
      int num = this.Height * 2 - history.Count() % 2;
      int count = 1;
      foreach (JsonElement note in history)
      {
        // TODO: use time data
        rev.Push((note[0].GetString(), count++));
      }
      this.Erase();
      count = 0;
      while (rev.Count != 0 && count < num)
      {
        count++;
        stack.Push(rev.Pop());
      }
      count = 0;
      foreach (var note in stack)
      {
        if (count % 2 == 0)
        {
          Ui.Pin(this.X, this.Y + (int) count / 2);
          if (this.Style == "wide")
          {
            num = (int) Math.Ceiling((double) note.Item2 / 2);
            Ui.Write($"{new string(' ', 3 - num.ToString().Length)}{num}. ");
          }
        }
        else
        {
          if (this.Style == "compact")
          {
            Ui.Pin(this.X + 7, this.Y + (int) count / 2);
          }
          else
          {
            Ui.Pin(this.X + 13, this.Y + (int) count / 2);
          }
        }
        Ui.Write(note.Item1);
        count++;
      }
    }
  }


  // PiecesElement: Displays the chess pieces, superposed onto the board.

  public class PiecesElement : Element
  {
    public PiecesElement(string style)
    {
      this.Style = style;
      switch (style)
      {
        case "compact":
          this.X = 16;
          this.Y = 6;
          this.Width = 8;
          this.Height = 8;
          break;
        case "wide":
          this.X = 25;
          this.Y = 8;
          this.Width = 29;
          this.Height = 15;
          break;
      }
    }

    public override void
    SetCfg(string cfg)
    {
      this.Style = Helper.JsonToCfgValue(cfg, "style");
      switch (this.Style)
      {
        case "compact":
          this.X = 16;
          this.Y = 6;
          this.Width = 8;
          this.Height = 8;
          break;
        case "wide":
          this.X = 25;
          this.Y = 8;
          this.Width = 29;
          this.Height = 15;
          break;
      }
    }

    public override void
    Draw(string state)
    {
      Ui.Pin(this.X, this.Y);
      foreach (JsonElement piece in
               Helper.JsonToStateListValue(state, "Live"))
      {
        var sym = piece[0].GetString();
        var black = piece[1].GetBoolean();
        var x = piece[2].GetInt32();
        var y = piece[3].GetInt32();
        string name = sym;
        if (black && sym == "p")
        {
          // pawn case disambiguation
          name = "o";
        }
        if (this.Style == "compact")
        {
          Ui.Pin(this.X + x - 1, this.Y + 8 - y);
          Ui.Write(name);
        }
        else if (this.Style == "wide")
        {
          Ui.Pin(this.X + 4 * x - 4, this.Y + 16 - 2 * y);
          Ui.Write(name);
        }
      }
    }
  }
}
