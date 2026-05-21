#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HomeworkViewer
{
    public class HistoryDialog : Form
    {
        private MonthCalendar calendar;
        private Button btnOk;
        private Button btnCancel;
        private List<DateTime> availableDates;
        private DateTime? selectedDate;

        public DateTime? SelectedDate => selectedDate;

        public HistoryDialog()
        {
            InitializeComponent();
            this.Load += HistoryDialog_Load;
            this.Resize += HistoryDialog_Resize;
        }

        private void InitializeComponent()
        {
            this.Text = "选择历史日期";
            this.Size = new Size(320, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            calendar = new MonthCalendar
            {
                Location = new Point(20, 20),
                CalendarDimensions = new Size(1, 1),
                MaxSelectionCount = 1,
                ShowToday = true,
                ShowTodayCircle = false,
                ShowWeekNumbers = false,
                TitleBackColor = Color.FromArgb(30, 30, 30),
                TitleForeColor = Color.White,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                TrailingForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9)
            };

            btnOk = new Button
            {
                Text = "确定",
                Location = new Point(80, 260),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(170, 260),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            calendar.DateSelected += Calendar_DateSelected;
            btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            btnCancel.Click += (s, e) => { selectedDate = null; DialogResult = DialogResult.Cancel; Close(); };

            this.Controls.Add(calendar);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
        }

        private void HistoryDialog_Load(object sender, EventArgs e)
        {
            CenterCalendarHorizontally();
            LoadData();
        }

        private void HistoryDialog_Resize(object sender, EventArgs e)
        {
            CenterCalendarHorizontally();
        }

        private void CenterCalendarHorizontally()
        {
            if (calendar != null)
            {
                int newLeft = (this.ClientSize.Width - calendar.Width) / 2;
                if (newLeft < 10) newLeft = 10;
                calendar.Left = newLeft;
            }
        }

        private void LoadData()
        {
            availableDates = HomeworkData.GetAvailableDates();
            if (availableDates.Count == 0)
            {
                btnOk.Enabled = false;
                selectedDate = null;
                calendar.BoldedDates = new DateTime[0];
                return;
            }

            calendar.BoldedDates = availableDates.ToArray();
            calendar.UpdateBoldedDates();

            DateTime defaultDate = availableDates[0];
            selectedDate = defaultDate;
            btnOk.Enabled = true;
            calendar.SetDate(defaultDate);
        }

        private void Calendar_DateSelected(object sender, DateRangeEventArgs e)
        {
            DateTime picked = e.Start;
            if (availableDates.Any(d => d.Date == picked.Date))
            {
                selectedDate = picked;
                btnOk.Enabled = true;
            }
            else
            {
                if (selectedDate.HasValue)
                    calendar.SetDate(selectedDate.Value);
                else if (availableDates.Count > 0)
                    calendar.SetDate(availableDates[0]);
                MessageBox.Show("所选日期没有作业数据，请选择加粗显示的日期。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}