using System;
using System.Collections.Generic;
using Sims3.SimIFace;
using Sims3.UI;

namespace Destrospean.UI.Dialogs
{
    public class ObjectPickerDialog : ModalDialog
    {
        public abstract class CommonHeaderInfo<T> : ObjectPicker.HeaderInfo
        {
            public virtual bool IsStub => false;

            public CommonHeaderInfo(string headerKey, string tooltipKey, int width) : base(headerKey, tooltipKey, width)
            {
            }

            public CommonHeaderInfo(string headerKey, string tooltipKey, int width, bool textIsImage) : base(headerKey, tooltipKey, width, textIsImage)
            {
            }

            public abstract ObjectPicker.ColumnInfo GetValue(T item);
        }

        private Button mOkayButton;

        private bool mOkayButtonAlwaysEnabled;

        private List<ObjectPicker.RowInfo> mResult;

        private int mSortedTab;

        private ObjectPicker mTable;

        private Vector2 mTableOffset;

        private bool mWasCancelled;

        private bool mWasConfirmed;

        public List<ObjectPicker.RowInfo> Result => mResult;

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
            var cancelButton = (Button)mModalDialogWindow.GetChildByID(99576786, false);
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

        private void OnSelectionChanged(List<ObjectPicker.RowInfo> selectedRows)
        {
            Audio.StartSound("ui_tertiary_button");
            mOkayButton.Enabled = mOkayButtonAlwaysEnabled || mTable.ObjectTable.SelectedItem > -1 && mTable.ObjectTable.GetRow(mTable.ObjectTable.SelectedItem) != null;
            OnTableChanged();
        }

        private void OnTableChanged()
        {
            if (mTable.ObjectTable.SelectedItem > -1 && mTable.ObjectTable.GetRow(mTable.ObjectTable.SelectedItem) != null && mTable.mTable.NumSelectableRows == 1)
            {
                EndDialog(mOkayButtonAlwaysEnabled ? SelectedID : OkayID);
            }
        }

        private void OnTabSelect(TabControl oldTab, TabControl newTab)
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
        }

        private void ResizeWindow(bool center)
        {
            var rowCount = ((uint)mModalDialogWindow.Parent.Area.Height - (uint)(mTableOffset.y * 2)) / mTable.mTable.RowHeight;
            mTable.mTable.VisibleRows = rowCount > mTable.mTable.NumberRows ? (uint)mTable.mTable.NumberRows : rowCount;
            mTable.mTable.GridSizeDirty = true;
            mTable.OnPopulationComplete();
            mModalDialogWindow.Area = new Rect(mModalDialogWindow.Area.TopLeft, mModalDialogWindow.Area.TopLeft + mTable.TableArea.BottomRight + mTableOffset);
            if (!center)
            {
                return;
            }
            var x = (float)Math.Round((mModalDialogWindow.Parent.Area.Width - mModalDialogWindow.Area.Width) / 2);
            var y = (float)Math.Round((mModalDialogWindow.Parent.Area.Height - mModalDialogWindow.Area.Height) / 2);
            mModalDialogWindow.Area = new Rect(x, y, x + mModalDialogWindow.Area.Width, y + mModalDialogWindow.Area.Height);
            var text = (Text)mModalDialogWindow.GetChildByID(99576787u, false);
            text.Area = new Rect(text.Area.TopLeft.x, 20f, text.Area.BottomRight.x, 50f - mModalDialogWindow.Area.Height);
            mModalDialogWindow.Visible = true;
            if (mSortedTab > -1 && mSortedTab != mTable.mSortedTab)
            {
                mTable.mTable.Sort(mSortedTab);
            }
        }

        public void OnComplete()
        {
            ResizeWindow(true);
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

        public static List<T> Show<T>(string title, List<ObjectPicker.TabInfo> tabs, List<CommonHeaderInfo<T>> headers, int selectableRowCount, out bool confirmed, out bool cancelled, bool okayButtonAlwaysEnabled = false, int sortedTab = 0)
        {
            return Show(title, tabs, headers, selectableRowCount, null, out confirmed, out cancelled, okayButtonAlwaysEnabled, sortedTab);
        }

        public static List<T> Show<T>(string title, List<ObjectPicker.TabInfo> tabs, List<CommonHeaderInfo<T>> headers, int selectableRowCount, List<ObjectPicker.RowInfo> preSelectedRows, out bool confirmed, out bool cancelled, bool okayButtonAlwaysEnabled = false, int sortedTab = 0)
        {
            Simulator.Sleep(0u);
            var columnInfoLists = new List<List<ObjectPicker.ColumnInfo>>();
            foreach (var tabInfo in tabs)
            {
                foreach (var rowInfo in tabInfo.RowInfo)
                {
                    if (columnInfoLists.Contains(rowInfo.ColumnInfo))
                    {
                        continue;
                    }
                    columnInfoLists.Add(rowInfo.ColumnInfo);
                    foreach (var headerInfo in headers)
                    {
                        var columnInfo = headerInfo.GetValue((T)rowInfo.Item);
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
            return Show<T>(title, tabs, headers.ConvertAll(x => (ObjectPicker.HeaderInfo)x), selectableRowCount, preSelectedRows, out confirmed, out cancelled, okayButtonAlwaysEnabled, sortedTab);
        }

        public static List<T> Show<T>(string title, List<ObjectPicker.TabInfo> tabs, List<ObjectPicker.HeaderInfo> headers, int selectableRowCount, List<ObjectPicker.RowInfo> preSelectedRows, out bool confirmed, out bool cancelled, bool okayButtonAlwaysEnabled = false, int sortedTab = -1)
        {
            using (var objectPickerDialog = new ObjectPickerDialog(title, tabs, headers, selectableRowCount, preSelectedRows, okayButtonAlwaysEnabled))
            {
                objectPickerDialog.mSortedTab = sortedTab;
                objectPickerDialog.StartModal();
                cancelled = objectPickerDialog.mWasCancelled;
                confirmed = objectPickerDialog.mWasConfirmed;
                if (objectPickerDialog.Result == null || objectPickerDialog.Result.Count == 0)
                {
                    return null;
                }
                var results = new List<T>();
                foreach (var rowInfo in objectPickerDialog.Result)
                {
                    results.Add((T)rowInfo.Item);
                }
                return results;
            }
        }
    }
}
