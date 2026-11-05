using System;
using System.Collections.Generic;
using Sims3.SimIFace;
﻿using Sims3.UI;

namespace Destrospean.UI.Dialogs
{
    public class ComboSelectionDialog : ModalDialog
    {
        private Button mCancelButton;

        private ComboBox mCombo;

        private Button mOkayButton;

        private object mResult;

        public object Result => mResult;

        public ComboSelectionDialog(string title, IDictionary<string, object> entries, object defaultEntry, Vector2 position, PauseMode pauseMode) : base("ComboSelectionDialog", 4096, true, pauseMode, null)
        {
            if (mModalDialogWindow == null)
            {
                return;
            }
            if (mModalDialogWindow.GetChildByID(5u, true) is Text text)
            {
                text.Caption = title;
            }
            mCombo = (ComboBox)mModalDialogWindow.GetChildByID(2u, true);
            foreach (var entry in entries)
            {
                mCombo.ValueList.Add(entry.Key, entry.Value);
                if (entry.Value == defaultEntry)
                {
                    mCombo.CurrentSelection = (uint)(mCombo.ValueList.Count - 1);
                }
            }
            var x = position.x;
            var y = position.y;
            if (x < 0 && y < 0)
            {
                x = (float)Math.Round((mModalDialogWindow.Parent.Area.Width - mModalDialogWindow.Area.Width) / 2);
                y = (float)Math.Round((mModalDialogWindow.Parent.Area.Height - mModalDialogWindow.Area.Height) / 2);
            }
            mModalDialogWindow.Area = new Rect(x, y, x + mModalDialogWindow.Area.Width, y + mModalDialogWindow.Area.Height);
            mOkayButton = mModalDialogWindow.GetChildByID(3u, false) as Button;
            if (mOkayButton != null)
            {
                mOkayButton.Click += OnButtonClick;
            }
            mCancelButton = mModalDialogWindow.GetChildByID(4u, false) as Button;
            if (mCancelButton != null)
            {
                mCancelButton.Click += OnButtonClick;
            }
            OkayID = 3u;
            SelectedID = OkayID;
            CancelID = 4u;
        }

        public void OnButtonClick(WindowBase sender, UIButtonClickEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            EndDialog(sender.ID);
        }

        public override bool OnEnd(uint buttonID)
        {
            mResult = buttonID == OkayID ? mCombo.EntryTags[(int)mCombo.CurrentSelection] : null;
            return true;
        }

        public static object Show(string title, IDictionary<string, object> entries, object defaultEntry)
        {
            return Show(title, entries, defaultEntry, new Vector2(-1, -1), PauseMode.PauseSimulator);
        }

        public static object Show(string title, IDictionary<string, object> entries, object defaultEntry, Vector2 position, PauseMode pauseMode)
        {
            if (EnableModalDialogs)
            {
                using (var comboSelectionDialog = new ComboSelectionDialog(title, entries, defaultEntry, position, pauseMode))
                {
                    comboSelectionDialog.StartModal();
                    return comboSelectionDialog.Result;
                }
            }
            return defaultEntry;
        }
    }
}
