﻿using System.Threading;

using STR.Common.Messages;


namespace UpkManager.Domain.Messages.FileHeader {

  public class LoadProgressMessage : MessageBase {

    #region Private Fields

    private int current;

    #endregion Private Fields

    #region Properties

    public string Text { get; set; }

    public int Current {
      get { return current; }
      set { current = value; }
    }

    public double Total { get; set; }

    public string StatusText { get; set; }

    public bool IsComplete { get; set; }

    #endregion Properties

    #region Public Methods

    public void IncrementCurrent() {
      Interlocked.Increment(ref current);
    }

    #endregion Public Methods

  }

}
