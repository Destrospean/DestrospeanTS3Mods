using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Collections.Generic;

namespace Destrospean.UI.Dialogs
{
    public class ObjectPickerDialog : ModalDialog
    {
        public abstract class CommonHeaderInfo<T> : ObjectPicker.HeaderInfo
        {
            public virtual bool IsStub
            {
                get
                {
                    return false;
                }
            }

            public CommonHeaderInfo(string headerKey, string tooltipKey, int width) : base(headerKey, tooltipKey, width)
            {
            }

            public CommonHeaderInfo(string headerKey, string tooltipKey, int width, bool textIsImage) : base(headerKey, tooltipKey, width, textIsImage)
            {
            }

            public abstract ObjectPicker.ColumnInfo GetValue(T item);
        }

        Button mOkayButton;

        bool mOkayButtonAlwaysEnabled;

        List<ObjectPicker.RowInfo> mResult;

        ObjectPicker mTable;

        Vector2 mTableOffset;

        bool mWasCancelled;

        bool mWasConfirmed;

        public List<ObjectPicker.RowInfo> Result
        {
            get
            {
                return mResult;
            }
        }

        public ObjectPickerDialog(string title, List<ObjectPicker.TabInfo> tabs, List<ObjectPicker.HeaderInfo> headers, int selectableRowCount, List<ObjectPicker.RowInfo> preSelectedRows, bool okayButtonAlwaysEnabled = false) : base("UiObjectPicker", 1, true, PauseMode.PauseSimulator, null)
        {
            mOkayButtonAlwaysEnabled = okayButtonAlwaysEnabled;
            if (mModalDialogWindow == null)
            {
                return;
            }
            mModalDialogWindow.Area = new Rect(new Vector2(mModalDialogWindow.Area.TopLeft.x, mModalDialogWindow.Area.TopLeft.y), new Vector2(mModalDialogWindow.Area.BottomRight.x + 200, mModalDialogWindow.Area.BottomRight.y));
            ((Text)mModalDialogWindow.GetChildByID(99576787u, false)).Caption = title;
            mTable = (ObjectPicker)mModalDialogWindow.GetChildByID(99576784u, false);
            mTable.ObjectTable.TableChanged += OnTableChanged;
            mTable.SelectionChanged += OnSelectionChanged;
            mTable.RowSelected += OnSelectionChanged;
            mTable.mViewButton.Visible = false;
            mTable.mTable.mPopulationCompletedCallback += OnComplete;
            mTable.Area = new Rect(new Vector2(mTable.Area.TopLeft.x, mTable.Area.TopLeft.y), new Vector2(mTable.Area.BottomRight.x + 200, mTable.Area.BottomRight.y));
            mOkayButton = (Button)mModalDialogWindow.GetChildByID(99576785u, false);
            mOkayButton.TooltipText = Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/Global:Accept");
            mOkayButton.Enabled = mOkayButtonAlwaysEnabled;
            mOkayButton.Click += (sender, eventArgs) =>
                {
                    eventArgs.Handled = true;
                    EndDialog(OkayID);
                };
            OkayID = mOkayButton.ID;
            SelectedID = 4;
            Button cancelButton = (Button)mModalDialogWindow.GetChildByID(99576786, false);
            cancelButton.TooltipText = Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:Cancel");
            cancelButton.Click += (sender, eventArgs) =>
                {
                    eventArgs.Handled = true;
                    EndDialog(CancelID);
                };
            CancelID = cancelButton.ID;
            mTableOffset = mModalDialogWindow.Area.BottomRight - mModalDialogWindow.Area.TopLeft - (mTable.Area.BottomRight - mTable.Area.TopLeft);
            mTable.Populate(tabs, headers, selectableRowCount);
            mTable.mTabs.TabSelect -= mTable.OnTabSelect;
            mTable.mTabs.TabSelect += OnTabSelect;
            mTable.ViewTypeToggle = true;
            ResizeWindow(true);
        }

        void OnSelectionChanged(List<ObjectPicker.RowInfo> selectedRows)
        {
            Audio.StartSound("ui_tertiary_button");
            mOkayButton.Enabled = mOkayButtonAlwaysEnabled || mTable.ObjectTable.SelectedItem > -1 && mTable.ObjectTable.GetRow(mTable.ObjectTable.SelectedItem) != null;
            OnTableChanged();
        }

        void OnTableChanged()
        {
            if (mTable.ObjectTable.SelectedItem > -1 && mTable.ObjectTable.GetRow(mTable.ObjectTable.SelectedItem) != null && mTable.mTable.NumSelectableRows == 1)
            {
                EndDialog(mOkayButtonAlwaysEnabled ? SelectedID : OkayID);
            }
        }

        void OnTabSelect(TabControl oldTab, TabControl newTab)
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    if (mTable.mSortedTab == (int)newTab.Tag)
                    {
                        return;
                    }
                    mTable.mSortedTab = (int)newTab.Tag;
                    mTable.mSortText.Caption = mTable.mItems[mTable.mSortedTab].TabText;
                    mTable.mTable.mPopulationCompletedCallback += mTable.OnPopulationComplete;
                    mTable.mTable.mPopulationCompletedCallback += OnComplete;
                    if (mTable.RepopulateTable())
                    {
                        OnComplete();
                    }
                });
        }

        void ResizeWindow(bool center)
        {
            uint rowCount = ((uint)mModalDialogWindow.Parent.Area.Height - (uint)(mTableOffset.y * 2)) / (uint)mTable.mTable.RowHeight;
            mTable.mTable.VisibleRows = rowCount > mTable.mTable.NumberRows ? (uint)mTable.mTable.NumberRows : rowCount;
            mTable.mTable.GridSizeDirty = true;
            mTable.OnPopulationComplete();
            mModalDialogWindow.Area = new Rect(mModalDialogWindow.Area.TopLeft, mModalDialogWindow.Area.TopLeft + mTable.TableArea.BottomRight + mTableOffset);
            if (!center)
            {
                return;
            }
            float x = (float)Math.Round((mModalDialogWindow.Parent.Area.Width - mModalDialogWindow.Area.Width) / 2);
            float y = (float)Math.Round((mModalDialogWindow.Parent.Area.Height - mModalDialogWindow.Area.Height) / 2);
            mModalDialogWindow.Area = new Rect(x, y, x + mModalDialogWindow.Area.Width, y + mModalDialogWindow.Area.Height);
            Text text = (Text)mModalDialogWindow.GetChildByID(99576787u, false);
            text.Area = new Rect(text.Area.TopLeft.x, 20f, text.Area.BottomRight.x, 50f - mModalDialogWindow.Area.Height);
            mModalDialogWindow.Visible = true;
        }

        public void OnComplete()
        {
            DebugUtils.TryDisplayScriptError(() => ResizeWindow(true));
        }

        public override bool OnEnd(uint endID)
        {
            if (endID == OkayID)
            {
                if (!mOkayButton.Enabled)
                {
                    return false;
                }
                mResult = mTable.Selected;
                mWasCancelled = false;
                mWasConfirmed = true;
            }
            else if (endID == SelectedID)
            {
                mResult = mTable.Selected;
                mWasCancelled = false;
                mWasConfirmed = false;
            }
            else
            {
                mResult = null;
                mWasCancelled = true;
                mWasConfirmed = false;
            }
            mTable.Populate(null, null, 0);
            return true;
        }

        public static List<T> Show<T>(string title, List<ObjectPicker.TabInfo> tabs, List<CommonHeaderInfo<T>> headers, int selectableRowCount, out bool confirmed, out bool cancelled, bool okayButtonAlwaysEnabled = false)
        {
            List<ObjectPicker.RowInfo> preSelectedRows = null;
            return Show(title, tabs, headers, selectableRowCount, preSelectedRows, out confirmed, out cancelled, okayButtonAlwaysEnabled);
        }

        public static List<T> Show<T>(string title, List<ObjectPicker.TabInfo> tabs, List<CommonHeaderInfo<T>> headers, int selectableRowCount, List<ObjectPicker.RowInfo> preSelectedRows, out bool confirmed, out bool cancelled, bool okayButtonAlwaysEnabled = false)
        {
            Simulator.Sleep(0u);
            List<List<ObjectPicker.ColumnInfo>> columnInfoLists = new List<List<ObjectPicker.ColumnInfo>>();
            foreach (ObjectPicker.TabInfo tabInfo in tabs)
            {
                foreach (ObjectPicker.RowInfo rowInfo in tabInfo.RowInfo)
                {
                    if (columnInfoLists.Contains(rowInfo.ColumnInfo))
                    {
                        continue;
                    }
                    columnInfoLists.Add(rowInfo.ColumnInfo);
                    foreach (CommonHeaderInfo<T> headerInfo in headers)
                    {
                        ObjectPicker.ColumnInfo columnInfo = headerInfo.GetValue((T)rowInfo.Item);
                        if (columnInfo == null)
                        {
                            if (headerInfo.IsStub)
                            {
                                continue;
                            }
                            columnInfo = new ObjectPicker.TextColumn("");
                        }
                        rowInfo.ColumnInfo.Add(columnInfo);
                    }
                }
            }
            return Show<T>(title, tabs, headers.ConvertAll(x => (ObjectPicker.HeaderInfo)x), selectableRowCount, preSelectedRows, out confirmed, out cancelled, okayButtonAlwaysEnabled);
        }

        public static List<T> Show<T>(string title, List<ObjectPicker.TabInfo> tabs, List<ObjectPicker.HeaderInfo> headers, int selectableRowCount, List<ObjectPicker.RowInfo> preSelectedRows, out bool confirmed, out bool cancelled, bool okayButtonAlwaysEnabled = false)
        {
            using (ObjectPickerDialog objectPickerDialog = new ObjectPickerDialog(title, tabs, headers, selectableRowCount, preSelectedRows, okayButtonAlwaysEnabled))
            {
                objectPickerDialog.StartModal();
                cancelled = objectPickerDialog.mWasCancelled;
                confirmed = objectPickerDialog.mWasConfirmed;
                if (objectPickerDialog.Result == null || objectPickerDialog.Result.Count == 0)
                {
                    return null;
                }
                List<T> results = new List<T>();
                foreach (ObjectPicker.RowInfo rowInfo in objectPickerDialog.Result)
                {
                    results.Add((T)rowInfo.Item);
                }
                return results;
            }
        }
    }
}
