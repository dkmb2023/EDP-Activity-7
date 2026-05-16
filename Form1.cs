using ClosedXML.Excel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ClinicSystem
{
    public partial class Form1 : Form
    {
        private Panel? mainContainer;
        private Panel? pnlLogin, pnlRecovery, pnlAppLayout;
        private Panel? pnlContentArea;

        Color clrMainBlue = Color.FromArgb(37, 99, 235);
        Color clrDarkBlue = Color.FromArgb(30, 41, 59);
        Color clrBg = Color.FromArgb(248, 250, 252);
        Color clrBorder = Color.FromArgb(226, 232, 240);
        Color clrTextGray = Color.FromArgb(100, 116, 139);
        Color clrGreen = Color.FromArgb(16, 185, 129);
        Color clrOrange = Color.FromArgb(245, 158, 11);
        Color clrPurple = Color.FromArgb(139, 92, 246);

        private string currentLoggedUser = "Guest";
        private string currentLoggedRole = "Staff";

        // ── Data Models ───────────────────────────────────────────────────────
        public class Patient
        {
            public string ID { get; set; } = "";
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public string Contact { get; set; } = "";
            public string Status { get; set; } = "";
        }

        public class Appointment
        {
            public int ID { get; set; }
            public string Time { get; set; } = "";
            public string Date { get; set; } = "";
            public string PatientName { get; set; } = "";
            public string Doctor { get; set; } = "";
            public string Type { get; set; } = "";
            public string Status { get; set; } = "Scheduled";
        }

        private List<Patient> patients = new List<Patient>();
        private List<Appointment> appointments = new List<Appointment>();

        private DataGridView? dgvPatients;
        private DataGridView? dgvUsers;

        // Real-time dashboard label references
        private Label? lblStatAppt, lblStatPatients, lblStatRevenue, lblStatToday;
        private System.Windows.Forms.Timer? dashTimer;

        // ── Password Hashing Helper ───────────────────────────────────────────
        // FIX #5: Passwords are now hashed with SHA-256 instead of stored plain text.
        // NOTE: For production, replace this with BCrypt (install BCrypt.Net-Next NuGet).
        private static string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ── Constructor ───────────────────────────────────────────────────────
        public Form1()
        {
            this.Size = new Size(1280, 850);
            this.Text = "MediFlow CMS Professional";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = clrBg;
            this.Font = new Font("Segoe UI", 10);
            this.DoubleBuffered = true;

            InitializeUI();

            // Seed patients
            patients.Add(new Patient { ID = "P-2026-001", Name = "Juan Dela Cruz", Age = 45, Contact = "0917-123-4567", Status = "In-Patient" });
            patients.Add(new Patient { ID = "P-2026-002", Name = "Maria Santos", Age = 29, Contact = "0922-888-9999", Status = "Out-Patient" });
            patients.Add(new Patient { ID = "P-2026-003", Name = "Ricardo Reyes", Age = 62, Contact = "0905-555-4432", Status = "Stable" });

            // Seed appointments
            string today = DateTime.Today.ToString("MM/dd/yyyy");
            appointments.Add(new Appointment { ID = 1, Date = today, Time = "09:00 AM", PatientName = "John Doe", Doctor = "Dr. Santos", Type = "General Checkup", Status = "Scheduled" });
            appointments.Add(new Appointment { ID = 2, Date = today, Time = "10:30 AM", PatientName = "Jane Smith", Doctor = "Dr. Reyes", Type = "Laboratory Result", Status = "Scheduled" });
            appointments.Add(new Appointment { ID = 3, Date = today, Time = "01:00 PM", PatientName = "Robert Brown", Doctor = "Dr. Santos", Type = "Follow-up", Status = "Done" });
            appointments.Add(new Appointment { ID = 4, Date = today, Time = "03:00 PM", PatientName = "Ana Gomez", Doctor = "Dr. Cruz", Type = "Consultation", Status = "Scheduled" });

            if (pnlLogin != null) ShowModule(pnlLogin);
        }

        // ── UI Bootstrap ──────────────────────────────────────────────────────
        private void InitializeUI()
        {
            mainContainer = new Panel { Dock = DockStyle.Fill };
            this.Controls.Add(mainContainer);
            CreateLoginModule();
            CreateRecoveryModule();
            // FIX #6: Removed premature CreateAppLayout() call here.
            // It is now only called after a successful login inside btnLogin.Click.
        }

        private void ShowModule(Panel module)
        {
            if (mainContainer == null) return;
            mainContainer.Controls.Clear();
            module.Dock = DockStyle.Fill;
            mainContainer.Controls.Add(module);
        }

        private Panel CreateCard(int w, int h)
        {
            Panel card = new Panel { Size = new Size(w, h), BackColor = Color.White, Padding = new Padding(20) };
            card.Resize += (s, e) => ApplyRoundedCorners(card, 12);
            return card;
        }

        private void StyleButton(Button b, bool isPrimary = true, Color? customColor = null)
        {
            Color baseColor = customColor ?? clrMainBlue;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Cursor = Cursors.Hand;
            b.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            if (isPrimary)
            {
                b.BackColor = baseColor;
                b.ForeColor = Color.White;
                b.MouseEnter += (s, e) => b.BackColor = ControlPaint.Dark(baseColor, 0.1f);
                b.MouseLeave += (s, e) => b.BackColor = baseColor;
            }
            else
            {
                b.BackColor = Color.Transparent;
                b.ForeColor = clrTextGray;
                b.TextAlign = ContentAlignment.MiddleLeft;
                b.Padding = new Padding(20, 0, 0, 0);
            }
        }

        private void ApplyRoundedCorners(Control control, int radius = 12)
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            Rectangle rect = new Rectangle(0, 0, control.Width, control.Height);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            control.Region = new Region(path);
        }

        private Button CreateBackButton()
        {
            Button back = new Button { Text = "Back", Width = 85, Height = 35, Location = new Point(20, 15) };
            StyleButton(back);
            back.Click += (s, e) => { dashTimer?.Stop(); pnlContentArea?.Controls.Clear(); LoadDashboard(); };
            return back;
        }

        // ── LOGIN MODULE ──────────────────────────────────────────────────────
        private void CreateLoginModule()
        {
            pnlLogin = new Panel { BackColor = clrBg };
            var card = CreateCard(400, 500);
            card.Location = new Point((1280 / 2) - 200, (850 / 2) - 250);

            var lblIcon = new Label { Text = "✚", ForeColor = clrMainBlue, Font = new Font("Segoe UI", 40), Dock = DockStyle.Top, Height = 80, TextAlign = ContentAlignment.MiddleCenter };
            var lblTitle = new Label { Text = "MediFlow", Font = new Font("Segoe UI", 22, FontStyle.Bold), Dock = DockStyle.Top, Height = 50, TextAlign = ContentAlignment.MiddleCenter };
            var txtUser = new TextBox { Width = 300, PlaceholderText = "Username", Font = new Font("Segoe UI", 12), Left = 50, Top = 200 };
            var txtPass = new TextBox { Width = 300, PlaceholderText = "Password", PasswordChar = '●', Font = new Font("Segoe UI", 12), Left = 50, Top = 260 };
            var btnLogin = new Button { Text = "Sign In", Width = 300, Height = 45, Left = 50, Top = 330 };
            StyleButton(btnLogin);
            var lnkRecover = new LinkLabel { Text = "Forgot Password?", Top = 400, Width = 400, TextAlign = ContentAlignment.MiddleCenter, LinkColor = clrMainBlue };

            btnLogin.Click += (s, e) =>
            {
                string uname = txtUser.Text.Trim();
                string ukey = txtPass.Text.Trim();
                if (string.IsNullOrEmpty(uname) || string.IsNullOrEmpty(ukey))
                {
                    MessageBox.Show("Please fill all fields.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    using (MySqlConnection conn = DBConnection.GetConnection())
                    {
                        // FIX #5: Compare hashed password
                        string hashedInput = HashPassword(ukey);
                        var cmd = new MySqlCommand(
                            "SELECT full_name, role, status FROM users WHERE username=@u AND password=@p", conn);
                        cmd.Parameters.AddWithValue("@u", uname);
                        cmd.Parameters.AddWithValue("@p", hashedInput);
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                if ((dr["status"]?.ToString() ?? "") != "Active")
                                {
                                    MessageBox.Show("Account is Inactive.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                    return;
                                }
                                currentLoggedUser = dr["full_name"]?.ToString() ?? "Unknown";
                                currentLoggedRole = dr["role"]?.ToString() ?? "Staff";
                                MessageBox.Show($"Welcome, {currentLoggedUser}!", "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                // FIX #6: CreateAppLayout() only called here after verified login
                                CreateAppLayout();
                                ShowModule(pnlAppLayout!);
                            }
                            else
                            {
                                MessageBox.Show("Invalid credentials.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Database Error"); }
            };

            lnkRecover.Click += (s, e) => { if (pnlRecovery != null) ShowModule(pnlRecovery); };
            card.Controls.AddRange(new Control[] { lblIcon, lblTitle, txtUser, txtPass, btnLogin, lnkRecover });
            pnlLogin.Controls.Add(card);
            txtUser.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { txtPass.Focus(); e.SuppressKeyPress = true; } };
            txtPass.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { btnLogin.PerformClick(); e.SuppressKeyPress = true; } };
        }

        // ── RECOVERY MODULE ───────────────────────────────────────────────────
        private void CreateRecoveryModule()
        {
            pnlRecovery = new Panel();
            var card = CreateCard(450, 360);
            card.Location = new Point(415, 200);

            var lblTitle = new Label { Text = "Account Recovery", Font = new Font("Segoe UI", 18, FontStyle.Bold), Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };
            var txtEmail = new TextBox { Width = 350, PlaceholderText = "Registered Email", Font = new Font("Segoe UI", 12), Left = 50, Top = 100 };
            var txtNewPass = new TextBox { Width = 350, PlaceholderText = "New Password", PasswordChar = '●', Font = new Font("Segoe UI", 12), Left = 50, Top = 155 };
            var btnSend = new Button { Text = "Reset Password", Width = 350, Height = 45, Left = 50, Top = 215, BackColor = clrDarkBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            var lnkBack = new LinkLabel { Text = "Back to Login", Top = 285, Width = 450, TextAlign = ContentAlignment.MiddleCenter };

            lnkBack.Click += (s, e) => { if (pnlLogin != null) ShowModule(pnlLogin); };
            btnSend.Click += (s, e) =>
            {
                string email = txtEmail.Text.Trim();
                string pass = txtNewPass.Text.Trim();
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) { MessageBox.Show("Fill all fields."); return; }
                try
                {
                    using (MySqlConnection conn = DBConnection.GetConnection())
                    {
                        // FIX #4: Use parameterized queries to prevent SQL injection
                        var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE email=@e", conn);
                        checkCmd.Parameters.AddWithValue("@e", email);
                        int cnt = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (cnt == 0) { MessageBox.Show("Email not found."); return; }

                        // FIX #5: Hash the new password before storing
                        string hashedNewPass = HashPassword(pass);
                        var updateCmd = new MySqlCommand("UPDATE users SET password=@p WHERE email=@e", conn);
                        updateCmd.Parameters.AddWithValue("@p", hashedNewPass);
                        updateCmd.Parameters.AddWithValue("@e", email);
                        updateCmd.ExecuteNonQuery();

                        MessageBox.Show("Password reset successfully.");
                        txtEmail.Clear(); txtNewPass.Clear();
                        ShowModule(pnlLogin!);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };

            card.Controls.AddRange(new Control[] { lblTitle, txtEmail, txtNewPass, btnSend, lnkBack });
            pnlRecovery.Controls.Add(card);
        }

        // ── APP LAYOUT ────────────────────────────────────────────────────────
        private void CreateAppLayout()
        {
            dashTimer?.Stop();
            dashTimer?.Dispose();
            dashTimer = null;

            pnlAppLayout = new Panel();

            Panel sidebar = new Panel { Width = 235, Dock = DockStyle.Left, BackColor = Color.White, Padding = new Padding(0, 10, 0, 0) };
            sidebar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(clrBorder), 234, 0, 234, sidebar.Height);

            sidebar.Controls.Add(new Label { Text = "✚ MediFlow", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = clrMainBlue, Dock = DockStyle.Top, Height = 55, TextAlign = ContentAlignment.MiddleCenter });

            // Role badge
            Color badge = currentLoggedRole == "Admin" ? Color.FromArgb(239, 68, 68) :
                          currentLoggedRole == "Doctor" ? Color.FromArgb(16, 185, 129) :
                                                          Color.FromArgb(99, 102, 241);
            sidebar.Controls.Add(new Label { Text = currentLoggedRole.ToUpper(), BackColor = badge, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Dock = DockStyle.Top, Height = 26, TextAlign = ContentAlignment.MiddleCenter });
            sidebar.Controls.Add(new Label { Text = currentLoggedUser, Font = new Font("Segoe UI", 9), ForeColor = clrTextGray, Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleCenter });

            // Role-specific menus
            List<string> menu;
            if (currentLoggedRole == "Admin") menu = new List<string> { "Dashboard", "Patients", "Appointments", "Reports", "Accounts System", "About System", "Logout" };
            else if (currentLoggedRole == "Doctor") menu = new List<string> { "Dashboard", "My Patients", "Appointments", "Reports", "About System", "Logout" };
            else menu = new List<string> { "Dashboard", "Patients", "Appointments", "Reports", "About System", "Logout" };

            foreach (var item in menu)
            {
                Button btn = new Button { Text = item, Dock = DockStyle.Top, Height = 48 };
                StyleButton(btn, false);
                btn.Click += Navigation_Click;
                sidebar.Controls.Add(btn);
                btn.BringToFront();
            }

            pnlContentArea = new Panel { Dock = DockStyle.Fill, BackColor = clrBg, Padding = new Padding(25) };
            pnlAppLayout.Controls.Add(pnlContentArea);
            pnlAppLayout.Controls.Add(sidebar);

            LoadDashboard();
        }

        private void Navigation_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && pnlContentArea != null)
            {
                dashTimer?.Stop();
                pnlContentArea.Controls.Clear();
                switch (btn.Text)
                {
                    case "Dashboard": LoadDashboard(); break;
                    case "Patients":
                    case "My Patients": LoadPatients(); break;
                    case "Appointments": LoadAppointments(); break;
                    case "Reports": LoadReports(); break;
                    case "Accounts System": LoadUserManagement(); break;
                    case "About System": LoadAbout(); break;
                    case "Logout":
                        // FIX #7: Properly stop AND dispose timer, then null it
                        dashTimer?.Stop();
                        dashTimer?.Dispose();
                        dashTimer = null;
                        ResetLoginFields();
                        if (pnlLogin != null) ShowModule(pnlLogin);
                        break;
                }
            }
        }

        private void ResetLoginFields()
        {
            if (pnlLogin == null) return;
            foreach (Control card in pnlLogin.Controls)
                foreach (Control ctrl in card.Controls)
                    if (ctrl is TextBox t) t.Clear();
        }

        // ══════════════════════════════════════════════════════════════════════
        // DASHBOARD  (role-aware + real-time 30s refresh)
        // ══════════════════════════════════════════════════════════════════════
        private void LoadDashboard()
        {
            if (pnlContentArea == null) return;

            string today = DateTime.Today.ToString("MM/dd/yyyy");
            string greeting = currentLoggedRole == "Doctor"
                ? $"Dr. {currentLoggedUser} — Dashboard"
                : $"Welcome back, {currentLoggedUser}";

            var lblTitle = new Label
            {
                Text = greeting,
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(pnlContentArea.Width - 50, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblTime = new Label
            {
                Text = "Last updated: " + DateTime.Now.ToString("hh:mm:ss tt"),
                ForeColor = clrTextGray,
                Font = new Font("Segoe UI", 9),
                Location = new Point(0, 50),
                AutoSize = true
            };
            lblStatToday = lblTime;

            // Compute stats
            int pendingCount = appointments.Count(a => a.Status == "Scheduled" && a.Date == today);
            int doneCount = appointments.Count(a => a.Status == "Done" && a.Date == today);
            int totalPatients = patients.Count;

            if (currentLoggedRole == "Doctor")
            {
                string lastName = currentLoggedUser.Split(' ').Last();
                pendingCount = appointments.Count(a => a.Status == "Scheduled" && a.Doctor.Contains(lastName));
                doneCount = appointments.Count(a => a.Status == "Done" && a.Doctor.Contains(lastName));
            }

            // ── Stat row ──
            int wx = 0, wy = 72, ww = 258, wh = 115, wgap = 18;

            // FIX #11: Use named tag to avoid Controls[1] fragility
            Panel wAppt = CreateStatWidgetFixed("Pending Today", pendingCount.ToString(), clrOrange, new Rectangle(wx, wy, ww, wh));
            Panel wPat = CreateStatWidgetFixed("Total Patients", totalPatients.ToString(), clrMainBlue, new Rectangle(wx + ww + wgap, wy, ww, wh));
            Panel wDone = CreateStatWidgetFixed(
                currentLoggedRole == "Doctor" ? "Completed Today" : "Today's Revenue",
                currentLoggedRole == "Doctor" ? doneCount.ToString() : "₱4,250",
                clrGreen, new Rectangle(wx + (ww + wgap) * 2, wy, ww, wh));

            lblStatAppt = (Label?)wAppt.Controls.Find("statValue", false).FirstOrDefault();
            lblStatPatients = (Label?)wPat.Controls.Find("statValue", false).FirstOrDefault();
            lblStatRevenue = (Label?)wDone.Controls.Find("statValue", false).FirstOrDefault();

            // ── Appointments table ──
            int contentTop = 205;
            int tableW = pnlContentArea.Width - 360;

            Panel pnlTable = new Panel
            {
                BackColor = Color.White,
                Location = new Point(0, contentTop),
                Size = new Size(tableW, 330),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlTable.Resize += (s, e) => ApplyRoundedCorners(pnlTable, 12);

            var lblTblTitle = new Label
            {
                Text = currentLoggedRole == "Doctor" ? "My Appointments Today" : "Appointments Today",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(14, 12),
                AutoSize = true
            };

            DataGridView dgvMini = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(tableW - 20, 275),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvMini.ColumnHeadersDefaultCellStyle.BackColor = clrBg;
            dgvMini.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvMini.Columns.Add("Time", "Time");
            dgvMini.Columns.Add("Patient", "Patient");
            dgvMini.Columns.Add("Doctor", "Doctor");
            dgvMini.Columns.Add("Type", "Type");
            dgvMini.Columns.Add("Status", "Status");

            dgvMini.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgvMini.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
                {
                    e.CellStyle.ForeColor = e.Value.ToString() == "Done" ? clrGreen :
                                            e.Value.ToString() == "Cancelled" ? Color.Red : clrOrange;
                    e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                }
            };

            Action refreshGrid = () =>
            {
                dgvMini.Rows.Clear();
                string tod = DateTime.Today.ToString("MM/dd/yyyy");
                var rows = currentLoggedRole == "Doctor"
                    ? appointments.Where(a => a.Doctor.Contains(currentLoggedUser.Split(' ').Last()))
                    : appointments.Where(a => a.Date == tod);
                foreach (var a in rows)
                    dgvMini.Rows.Add(a.Time, a.PatientName, a.Doctor, a.Type, a.Status);
            };
            refreshGrid();

            pnlTable.Controls.Add(lblTblTitle);
            pnlTable.Controls.Add(dgvMini);

            // ── Quick actions ──
            Panel pnlAct = new Panel
            {
                BackColor = Color.White,
                Location = new Point(tableW + 15, contentTop),
                Size = new Size(pnlContentArea.Width - tableW - 50, 330),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlAct.Resize += (s, e) => ApplyRoundedCorners(pnlAct, 12);
            pnlAct.Controls.Add(new Label { Text = "Quick Actions", Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(14, 12), AutoSize = true });

            var quickBtns = currentLoggedRole == "Doctor"
                ? new (string, Color)[] { ("New Appointment", clrMainBlue), ("My Patients", clrGreen), ("View Reports", clrPurple) }
                : new (string, Color)[] { ("New Appointment", clrMainBlue), ("Add Patient", clrGreen), ("View Reports", clrPurple) };

            int qy = 55;
            foreach (var (txt, col) in quickBtns)
            {
                Button bq = new Button { Text = "＋ " + txt, Location = new Point(14, qy), Width = pnlAct.Width - 28, Height = 42 };
                StyleButton(bq, true, col);
                string captured = txt;
                bq.Click += (s, e) =>
                {
                    dashTimer?.Stop();
                    pnlContentArea?.Controls.Clear();
                    if (captured.Contains("Appointment")) ShowAddAppointmentForm();
                    // FIX #8: Navigate to Patients module — don't call both ShowAddPatientForm + LoadPatients
                    // ShowAddPatientForm is modal (blocking), so after it closes LoadPatients shows updated list.
                    else if (captured.Contains("Patient")) { ShowAddPatientForm(); LoadPatients(); }
                    else if (captured.Contains("Reports")) LoadReports();
                };
                pnlAct.Controls.Add(bq);
                qy += 56;
            }

            pnlContentArea.Controls.AddRange(new Control[] { lblTitle, lblTime, wAppt, wPat, wDone, pnlTable, pnlAct });

            // ── Real-time timer ──
            dashTimer?.Stop(); dashTimer?.Dispose();
            dashTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            dashTimer.Tick += (s, e) =>
            {
                string t2 = DateTime.Today.ToString("MM/dd/yyyy");
                int p2 = appointments.Count(a => a.Status == "Scheduled" && a.Date == t2);
                int d2 = appointments.Count(a => a.Status == "Done" && a.Date == t2);
                if (lblStatAppt != null) lblStatAppt.Text = p2.ToString();
                if (lblStatPatients != null) lblStatPatients.Text = patients.Count.ToString();
                if (lblStatRevenue != null) lblStatRevenue.Text = currentLoggedRole == "Doctor" ? d2.ToString() : "₱4,250";
                if (lblStatToday != null) lblStatToday.Text = "Last updated: " + DateTime.Now.ToString("hh:mm:ss tt");
                refreshGrid();
            };
            dashTimer.Start();
        }

        // FIX #11: Named the value label "statValue" so we can find it by name instead of index
        private Panel CreateStatWidgetFixed(string title, string val, Color accent, Rectangle bounds)
        {
            Panel p = new Panel { BackColor = Color.White, Bounds = bounds, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            p.Paint += (s, e) => { using (SolidBrush b = new SolidBrush(accent)) e.Graphics.FillRectangle(b, 0, 0, 5, p.Height); };
            p.Resize += (s, e) => ApplyRoundedCorners(p, 12);
            p.Controls.Add(new Label { Text = title, ForeColor = clrTextGray, Location = new Point(16, 14), AutoSize = true, Font = new Font("Segoe UI", 9) });
            var valLabel = new Label { Name = "statValue", Text = val, Font = new Font("Segoe UI", 24, FontStyle.Bold), Location = new Point(16, 40), AutoSize = true };
            p.Controls.Add(valLabel);
            return p;
        }

        // ══════════════════════════════════════════════════════════════════════
        // APPOINTMENTS MODULE
        // ══════════════════════════════════════════════════════════════════════
        private void LoadAppointments()
        {
            if (pnlContentArea == null) return;
            pnlContentArea.Controls.Clear();

            Button btnBack = CreateBackButton();
            Label title = new Label { Text = "Appointments", Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(115, 15), AutoSize = true };

            Panel filterBar = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20, 58),
                Size = new Size(pnlContentArea.Width - 40, 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            filterBar.Resize += (s, e) => ApplyRoundedCorners(filterBar, 10);

            Label lblFDate = new Label { Text = "📅  Date", ForeColor = clrTextGray, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(16, 8), AutoSize = true };
            var dtp = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Location = new Point(16, 26),
                Width = 145,
                Font = new Font("Segoe UI", 10),
                CalendarForeColor = clrDarkBlue,
                CalendarMonthBackground = clrBg
            };

            Panel div1 = new Panel { BackColor = clrBorder, Location = new Point(174, 8), Size = new Size(1, 36) };

            Label lblFStat = new Label { Text = "🔖  Status", ForeColor = clrTextGray, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(186, 8), AutoSize = true };
            var cmbF = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(186, 26),
                Width = 140,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat
            };
            cmbF.Items.AddRange(new string[] { "All", "Scheduled", "Done", "Cancelled" });
            cmbF.SelectedIndex = 0;

            filterBar.Controls.AddRange(new Control[] { lblFDate, dtp, div1, lblFStat, cmbF });

            var dgv = new DataGridView
            {
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(20, 120),
                Size = new Size(pnlContentArea.Width - 40, 285),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = clrBg;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.Columns.Add("ApptID", "ID");
            dgv.Columns.Add("Date", "Date");
            dgv.Columns.Add("Time", "Time");
            dgv.Columns.Add("Patient", "Patient");
            dgv.Columns.Add("Doctor", "Doctor");
            dgv.Columns.Add("Type", "Type");
            dgv.Columns.Add("Status", "Status");

            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "Status" && e.Value != null)
                {
                    e.CellStyle.ForeColor = e.Value.ToString() == "Done" ? clrGreen :
                                            e.Value.ToString() == "Cancelled" ? Color.Red : clrOrange;
                    e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                }
            };

            Action populate = () =>
            {
                dgv.Rows.Clear();
                string selDate = dtp.Value.ToString("MM/dd/yyyy");
                string selStat = cmbF.Text;
                var rows = appointments.Where(a => a.Date == selDate && (selStat == "All" || a.Status == selStat));
                if (currentLoggedRole == "Doctor")
                    rows = rows.Where(a => a.Doctor.Contains(currentLoggedUser.Split(' ').Last()));
                foreach (var a in rows)
                    dgv.Rows.Add(a.ID, a.Date, a.Time, a.PatientName, a.Doctor, a.Type, a.Status);
            };

            populate();
            dtp.ValueChanged += (s, e) => populate();
            cmbF.SelectedIndexChanged += (s, e) => populate();

            int bTop = 420, bH = 40;
            Button btnAdd = new Button { Text = "＋ New", Location = new Point(20, bTop), Width = 120, Height = bH };
            StyleButton(btnAdd, true, clrMainBlue);
            btnAdd.Click += (s, e) => { ShowAddAppointmentForm(); populate(); };

            Button btnDone = new Button { Text = "✔ Done", Location = new Point(150, bTop), Width = 120, Height = bH };
            StyleButton(btnDone, true, clrGreen);
            btnDone.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Select a row first."); return; }
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ApptID"].Value);
                var a = appointments.FirstOrDefault(x => x.ID == id);
                if (a != null) { a.Status = "Done"; populate(); }
            };

            Button btnCancel = new Button { Text = "✖ Cancel", Location = new Point(280, bTop), Width = 120, Height = bH };
            StyleButton(btnCancel, true, Color.FromArgb(239, 68, 68));
            btnCancel.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Select a row first."); return; }
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ApptID"].Value);
                var a = appointments.FirstOrDefault(x => x.ID == id);
                if (a != null) { a.Status = "Cancelled"; populate(); }
            };

            Button btnDel = new Button { Text = "🗑 Delete", Location = new Point(410, bTop), Width = 120, Height = bH };
            StyleButton(btnDel, true, clrDarkBlue);
            btnDel.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Select a row first."); return; }
                int id = Convert.ToInt32(dgv.SelectedRows[0].Cells["ApptID"].Value);
                var a = appointments.FirstOrDefault(x => x.ID == id);
                if (a != null && MessageBox.Show("Delete this appointment?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                { appointments.Remove(a); populate(); }
            };

            pnlContentArea.Controls.AddRange(new Control[] { title, btnBack, filterBar, dgv, btnAdd, btnDone, btnCancel, btnDel });
        }

        private void ShowAddAppointmentForm()
        {
            Form modal = new Form
            {
                Text = "New Appointment",
                Size = new Size(420, 430),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = clrBg
            };

            Label L(string t, int y) => new Label { Text = t, Location = new Point(30, y), AutoSize = true, Font = new Font("Segoe UI", 9) };
            TextBox T(string p, int y) => new TextBox { PlaceholderText = p, Location = new Point(30, y), Width = 340, Font = new Font("Segoe UI", 11) };

            var txtPat = T("Patient Full Name", 40);
            var txtDoc = T("Doctor (e.g. Dr. Santos)", 110);
            var txtType = T("Consultation Type", 180);
            var txtTime = new TextBox { PlaceholderText = "Time (e.g. 09:00 AM)", Location = new Point(195, 250), Width = 175, Font = new Font("Segoe UI", 11) };
            var dtp = new DateTimePicker { Location = new Point(30, 250), Width = 155, Format = DateTimePickerFormat.Short };

            Button btnSave = new Button { Text = "Save Appointment", Location = new Point(30, 320), Width = 340, Height = 48, BackColor = clrMainBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPat.Text) || string.IsNullOrWhiteSpace(txtDoc.Text))
                { MessageBox.Show("Patient Name and Doctor are required."); return; }
                // FIX #9: Collision-safe ID using Max + 1
                int nid = appointments.Count > 0 ? appointments.Max(a => a.ID) + 1 : 1;
                appointments.Add(new Appointment
                {
                    ID = nid,
                    Date = dtp.Value.ToString("MM/dd/yyyy"),
                    Time = string.IsNullOrWhiteSpace(txtTime.Text) ? "TBD" : txtTime.Text.Trim(),
                    PatientName = txtPat.Text.Trim(),
                    Doctor = txtDoc.Text.Trim(),
                    Type = string.IsNullOrWhiteSpace(txtType.Text) ? "Consultation" : txtType.Text.Trim(),
                    Status = "Scheduled"
                });
                MessageBox.Show("Appointment saved successfully!", "Success");
                modal.Close();
            };

            modal.Controls.AddRange(new Control[] { L("Patient Name", 20), txtPat, L("Doctor", 90), txtDoc, L("Type", 160), txtType, L("Date", 230), dtp, L("Time", 230), txtTime, btnSave });
            modal.ShowDialog();
        }

        // ══════════════════════════════════════════════════════════════════════
        // PATIENTS MODULE
        // ══════════════════════════════════════════════════════════════════════
        private void LoadPatients()
        {
            if (pnlContentArea == null) return;
            pnlContentArea.Controls.Clear();

            Button btnBack = CreateBackButton();
            Label title = new Label { Text = currentLoggedRole == "Doctor" ? "My Patients" : "Patient Registry", Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(115, 15), AutoSize = true };

            dgvPatients = new DataGridView
            {
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(20, 65),
                Size = new Size(pnlContentArea.Width - 40, 340),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvPatients.ColumnHeadersDefaultCellStyle.BackColor = clrBg;
            dgvPatients.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvPatients.Columns.Add("ID", "ID");
            dgvPatients.Columns.Add("Name", "Name");
            dgvPatients.Columns.Add("Age", "Age");
            dgvPatients.Columns.Add("Contact", "Contact");
            dgvPatients.Columns.Add("Status", "Status");

            foreach (var p in patients)
                dgvPatients.Rows.Add(p.ID, p.Name, p.Age, p.Contact, p.Status);

            pnlContentArea.Controls.AddRange(new Control[] { title, dgvPatients, btnBack });

            if (currentLoggedRole != "Doctor")
            {
                Button btnAdd = new Button { Text = "＋ Add Patient", Location = new Point(20, 420), Width = 155, Height = 40 };
                StyleButton(btnAdd, true, clrGreen);
                btnAdd.Click += (s, e) =>
                {
                    ShowAddPatientForm();
                    // Refresh grid after modal closes
                    if (dgvPatients != null)
                    {
                        dgvPatients.Rows.Clear();
                        foreach (var p in patients) dgvPatients.Rows.Add(p.ID, p.Name, p.Age, p.Contact, p.Status);
                    }
                };
                pnlContentArea.Controls.Add(btnAdd);
            }
        }

        private void ShowAddPatientForm()
        {
            Form modal = new Form
            {
                Text = "Register Patient",
                Size = new Size(400, 390),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            TextBox txtName = new TextBox { PlaceholderText = "Full Name", Top = 30, Left = 30, Width = 320 };
            TextBox txtAge = new TextBox { PlaceholderText = "Age", Top = 80, Left = 30, Width = 320 };
            TextBox txtContact = new TextBox { PlaceholderText = "09XX-XXX-XXXX", Top = 130, Left = 30, Width = 320 };

            bool _fmt = false;
            txtContact.TextChanged += (s, e) =>
            {
                if (_fmt) return; _fmt = true;
                string d = new string(txtContact.Text.Where(char.IsDigit).ToArray());
                if (d.Length > 11) d = d.Substring(0, 11);
                string f = d.Length <= 4 ? d
                         : d.Length <= 7 ? d.Substring(0, 4) + "-" + d.Substring(4)
                         : d.Substring(0, 4) + "-" + d.Substring(4, 3) + "-" + d.Substring(7);
                txtContact.Text = f; txtContact.SelectionStart = f.Length;
                _fmt = false;
            };

            ComboBox cmbStatus = new ComboBox { Top = 180, Left = 30, Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new string[] { "In-Patient", "Out-Patient", "Stable" });
            cmbStatus.SelectedIndex = 1;

            Button btnSave = new Button { Text = "Save Patient", Top = 250, Left = 30, Width = 320, Height = 45, BackColor = clrMainBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Name is required."); return; }
                // FIX #9: Collision-safe ID — use Max numeric suffix + 1 instead of Count + 1
                int nextNum = patients.Count > 0
                    ? patients.Select(p => { int.TryParse(p.ID.Split('-').Last(), out int n); return n; }).Max() + 1
                    : 1;
                patients.Add(new Patient
                {
                    ID = "P-2026-" + nextNum.ToString("D3"),
                    Name = txtName.Text.Trim(),
                    Age = int.TryParse(txtAge.Text, out int r) ? r : 0,
                    Contact = txtContact.Text.Trim(),
                    Status = cmbStatus.Text
                });
                modal.Close();
            };

            modal.Controls.AddRange(new Control[] { txtName, txtAge, txtContact, cmbStatus, btnSave });
            modal.ShowDialog();
        }

        // ══════════════════════════════════════════════════════════════════════
        // REPORTS MODULE
        // ══════════════════════════════════════════════════════════════════════

        private void LoadReports()
        {
            if (pnlContentArea == null) return;
            pnlContentArea.Controls.Clear();

            Button btnBack = CreateBackButton();
            Label title = new Label
            {
                Text = "Reports & Analytics",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                Location = new Point(115, 15),
                AutoSize = true
            };

            // ── Filter bar ──────────────────────────────────────────────────────
            Panel filterBar = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20, 58),
                Size = new Size(pnlContentArea.Width - 40, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            filterBar.Resize += (s, e) => ApplyRoundedCorners(filterBar, 10);

            // From date
            Label lblFrom = new Label { Text = "📅  From", ForeColor = clrTextGray, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(16, 6), AutoSize = true };
            var dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Location = new Point(16, 28), Width = 138, Font = new Font("Segoe UI", 10) };

            Panel div1 = new Panel { BackColor = clrBorder, Location = new Point(164, 8), Size = new Size(1, 44) };

            // To date
            Label lblTo = new Label { Text = "📅  To", ForeColor = clrTextGray, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(174, 6), AutoSize = true };
            var dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Location = new Point(174, 28), Width = 138, Font = new Font("Segoe UI", 10) };

            Panel div2 = new Panel { BackColor = clrBorder, Location = new Point(322, 8), Size = new Size(1, 44) };

            // Report type dropdown
            Label lblType = new Label { Text = "📋  Report Type", ForeColor = clrTextGray, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(332, 6), AutoSize = true };
            var cmbReportType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(332, 28),
                Width = 148,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat
            };
            cmbReportType.Items.AddRange(new string[] { "Consultations", "Billings", "Prescriptions" });
            cmbReportType.SelectedIndex = 0;

            Panel div3 = new Panel { BackColor = clrBorder, Location = new Point(490, 8), Size = new Size(1, 44) };

            // Load Records button
            var btnLoad = new Button
            {
                Text = "Load Records",
                Location = new Point(500, 16),
                Width = 140,
                Height = 32,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            StyleButton(btnLoad, true, clrMainBlue);

            // Export button
            var btnExport = new Button
            {
                Text = "⬇ Export Excel",
                Location = new Point(650, 16),
                Width = 145,
                Height = 32,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            StyleButton(btnExport, true, clrGreen);

            filterBar.Controls.AddRange(new Control[] {
        lblFrom, dtpFrom, div1, lblTo, dtpTo, div2, lblType, cmbReportType, div3, btnLoad, btnExport
    });

            // ── Summary card ─────────────────────────────────────────────────────
            Panel cardSum = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20, 130),
                Size = new Size(pnlContentArea.Width - 40, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cardSum.Resize += (s, e) => ApplyRoundedCorners(cardSum, 12);
            cardSum.Controls.Add(new Label { Text = "Summary", Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(15, 8), AutoSize = true });

            Label lblSumA = new Label { Location = new Point(15, 42), AutoSize = true, Font = new Font("Segoe UI", 9) };
            Label lblSumB = new Label { Location = new Point(230, 42), AutoSize = true, Font = new Font("Segoe UI", 9) };
            Label lblSumC = new Label { Location = new Point(450, 42), AutoSize = true, Font = new Font("Segoe UI", 9) };
            cardSum.Controls.AddRange(new Control[] { lblSumA, lblSumB, lblSumC });

            // ── Detail DataGridView ───────────────────────────────────────────────
            DataGridView dgvReports = new DataGridView
            {
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Location = new Point(20, 222),
                Size = new Size(pnlContentArea.Width - 40, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DefaultCellStyle = { SelectionBackColor = Color.FromArgb(219, 234, 254), SelectionForeColor = Color.FromArgb(30, 41, 59) }
            };
            dgvReports.ColumnHeadersDefaultCellStyle.BackColor = clrBg;
            dgvReports.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            // Status label
            Label lblStatus = new Label
            {
                Text = "Select a report type and click Load Records.",
                ForeColor = clrTextGray,
                Font = new Font("Segoe UI", 9),
                Location = new Point(20, 530),
                AutoSize = true
            };

            // ── Load Records logic ────────────────────────────────────────────────
            btnLoad.Click += (s, e) =>
            {
                dgvReports.Rows.Clear();
                dgvReports.Columns.Clear();
                lblSumA.Text = "";
                lblSumB.Text = "";
                lblSumC.Text = "";
                lblStatus.Text = "Loading...";

                string from = dtpFrom.Value.ToString("yyyy-MM-dd 00:00:00");
                string to = dtpTo.Value.ToString("yyyy-MM-dd 23:59:59");

                try
                {
                    using (MySqlConnection conn = DBConnection.GetConnection())
                    {
                        switch (cmbReportType.Text)
                        {
                            // ── CONSULTATIONS ──────────────────────────────────
                            case "Consultations":
                                {
                                    dgvReports.Columns.Add("appt_id", "ID");
                                    dgvReports.Columns.Add("date", "Date");
                                    dgvReports.Columns.Add("patient", "Patient");
                                    dgvReports.Columns.Add("doctor", "Doctor");
                                    dgvReports.Columns.Add("spec", "Specialization");
                                    dgvReports.Columns.Add("status", "Status");

                                    // Color-code Status column
                                    dgvReports.CellFormatting += (cs, ce) =>
                                    {
                                        if (ce.ColumnIndex >= 0 && dgvReports.Columns[ce.ColumnIndex].Name == "status" && ce.Value != null)
                                        {
                                            ce.CellStyle.ForeColor = ce.Value.ToString() == "Completed" ? clrGreen :
                                                                     ce.Value.ToString() == "Cancelled" ? Color.Red : clrOrange;
                                            ce.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                                        }
                                    };

                                    string sql = @"
                                        SELECT
                                            a.appointment_id,
                                            DATE_FORMAT(a.appointment_date, '%m/%d/%Y') AS appt_date,
                                            CONCAT(p.first_name, ' ', p.last_name)      AS patient_name,
                                            CONCAT(d.first_name, ' ', d.last_name)      AS doctor_name,
                                            d.specialization,
                                            a.status
                                        FROM appointments a
                                        JOIN patients p ON a.patient_id  = p.patient_id
                                        JOIN doctors  d ON a.doctor_id   = d.doctor_id
                                        WHERE a.appointment_date BETWEEN @from AND @to
                                        ORDER BY a.appointment_date DESC";

                                    var cmd = new MySqlCommand(sql, conn);
                                    cmd.Parameters.AddWithValue("@from", from);
                                    cmd.Parameters.AddWithValue("@to", to);

                                    int total = 0, completed = 0, cancelled = 0;
                                    using (var dr = cmd.ExecuteReader())
                                    {
                                        while (dr.Read())
                                        {
                                            dgvReports.Rows.Add(
                                                dr["appointment_id"],
                                                dr["appt_date"],
                                                dr["patient_name"],
                                                dr["doctor_name"],
                                                dr["specialization"],
                                                dr["status"]);
                                            total++;
                                            if (dr["status"].ToString() == "Completed") completed++;
                                            if (dr["status"].ToString() == "Cancelled") cancelled++;
                                        }
                                    }
                                    lblSumA.Text = $"Total Appointments : {total}";
                                    lblSumB.Text = $"Completed          : {completed}";
                                    lblSumC.Text = $"Cancelled          : {cancelled}";
                                    lblStatus.Text = $"✔ {total} record(s) loaded.";
                                    break;
                                }

                            // ── BILLINGS ───────────────────────────────────────
                            case "Billings":
                                {
                                    dgvReports.Columns.Add("pay_id", "Payment ID");
                                    dgvReports.Columns.Add("date", "Payment Date");
                                    dgvReports.Columns.Add("patient", "Patient");
                                    dgvReports.Columns.Add("doctor", "Doctor");
                                    dgvReports.Columns.Add("amount", "Amount Paid (₱)");
                                    dgvReports.Columns.Add("method", "Method");

                                    string sql = @"
                                        SELECT
                                            py.payment_id,
                                            DATE_FORMAT(py.payment_date, '%m/%d/%Y')    AS pay_date,
                                            CONCAT(p.first_name, ' ', p.last_name)      AS patient_name,
                                            CONCAT(d.first_name, ' ', d.last_name)      AS doctor_name,
                                            py.amount_paid,
                                            py.payment_method
                                        FROM payments py
                                        JOIN appointments a ON py.appointment_id = a.appointment_id
                                        JOIN patients     p ON a.patient_id      = p.patient_id
                                        JOIN doctors      d ON a.doctor_id       = d.doctor_id
                                        WHERE py.payment_date BETWEEN @from AND @to
                                        ORDER BY py.payment_date DESC";

                                    var cmd = new MySqlCommand(sql, conn);
                                    cmd.Parameters.AddWithValue("@from", from);
                                    cmd.Parameters.AddWithValue("@to", to);

                                    int total = 0;
                                    decimal grandTotal = 0;
                                    string topMethod = "";
                                    var methodCount = new Dictionary<string, int>();

                                    using (var dr = cmd.ExecuteReader())
                                    {
                                        while (dr.Read())
                                        {
                                            decimal amt = Convert.ToDecimal(dr["amount_paid"]);
                                            string mth = dr["payment_method"].ToString() ?? "";
                                            dgvReports.Rows.Add(
                                                dr["payment_id"],
                                                dr["pay_date"],
                                                dr["patient_name"],
                                                dr["doctor_name"],
                                                amt.ToString("N2"),
                                                mth);
                                            total++;
                                            grandTotal += amt;
                                            if (!methodCount.ContainsKey(mth)) methodCount[mth] = 0;
                                            methodCount[mth]++;
                                        }
                                    }
                                    if (methodCount.Count > 0)
                                        topMethod = methodCount.OrderByDescending(x => x.Value).First().Key;

                                    lblSumA.Text = $"Total Transactions : {total}";
                                    lblSumB.Text = $"Total Revenue      : ₱{grandTotal:N2}";
                                    lblSumC.Text = $"Top Method         : {(string.IsNullOrEmpty(topMethod) ? "N/A" : topMethod)}";
                                    lblStatus.Text = $"✔ {total} record(s) loaded.";
                                    break;
                                }

                            // ── PRESCRIPTIONS (Treatments) ─────────────────────
                            case "Prescriptions":
                                {
                                    dgvReports.Columns.Add("treat_id", "Treatment ID");
                                    dgvReports.Columns.Add("date", "Date");
                                    dgvReports.Columns.Add("patient", "Patient");
                                    dgvReports.Columns.Add("doctor", "Doctor");
                                    dgvReports.Columns.Add("desc", "Description");
                                    dgvReports.Columns.Add("cost", "Cost (₱)");

                                    string sql = @"
                                        SELECT
                                            t.treatment_id,
                                            DATE_FORMAT(a.appointment_date, '%m/%d/%Y') AS appt_date,
                                            CONCAT(p.first_name, ' ', p.last_name)      AS patient_name,
                                            CONCAT(d.first_name, ' ', d.last_name)      AS doctor_name,
                                            t.description,
                                            t.cost
                                        FROM treatments t
                                        JOIN appointments a ON t.appointment_id = a.appointment_id
                                        JOIN patients     p ON a.patient_id     = p.patient_id
                                        JOIN doctors      d ON a.doctor_id      = d.doctor_id
                                        WHERE a.appointment_date BETWEEN @from AND @to
                                        ORDER BY a.appointment_date DESC";

                                    var cmd = new MySqlCommand(sql, conn);
                                    cmd.Parameters.AddWithValue("@from", from);
                                    cmd.Parameters.AddWithValue("@to", to);

                                    int total = 0;
                                    decimal totalCost = 0;
                                    using (var dr = cmd.ExecuteReader())
                                    {
                                        while (dr.Read())
                                        {
                                            decimal cost = Convert.ToDecimal(dr["cost"]);
                                            dgvReports.Rows.Add(
                                                dr["treatment_id"],
                                                dr["appt_date"],
                                                dr["patient_name"],
                                                dr["doctor_name"],
                                                dr["description"],
                                                cost.ToString("N2"));
                                            total++;
                                            totalCost += cost;
                                        }
                                    }
                                    lblSumA.Text = $"Total Treatments   : {total}";
                                    lblSumB.Text = $"Total Cost         : ₱{totalCost:N2}";
                                    lblSumC.Text = "";
                                    lblStatus.Text = $"✔ {total} record(s) loaded.";
                                    break;
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "⚠ Error loading records.";
                    MessageBox.Show("DB Error: " + ex.Message, "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // ── Export to Excel logic ─────────────────────────────────────────────
            btnExport.Click += (s, e) =>
            {
                if (dgvReports.Rows.Count == 0)
                {
                    MessageBox.Show("No data to export. Click Load Records first.", "Nothing to Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string templateFolder = @"C:\ClinicTemplates\";
                string templateFile = cmbReportType.Text switch
                {
                    "Consultations" => "consultation_template.xlsx",
                    "Billings" => "billing_template.xlsx",
                    "Prescriptions" => "prescription_template.xlsx",
                    _ => ""
                };
                string templatePath = Path.Combine(templateFolder, templateFile);

                try
                {
                    XLWorkbook workbook;
                    IXLWorksheet worksheet;

                    if (File.Exists(templatePath))
                    {
                        // Template-based export
                        workbook = new XLWorkbook(templatePath);
                        worksheet = workbook.Worksheet(1);

                        // Magsimula sa Row 7 base sa iyong adjustment
                        int startRow = 7;

                        // Simpleng loop para ipasok ang data nang diretso at walang merging sa table cells
                        for (int i = 0; i < dgvReports.Rows.Count; i++)
                        {
                            for (int j = 0; j < dgvReports.Columns.Count; j++)
                            {
                                worksheet.Cell(startRow + i, j + 1).Value =
                                    dgvReports.Rows[i].Cells[j].Value?.ToString() ?? "";
                            }
                        }

                        // ── FOOTER ROW CONFIGURATION ─────────────────────────────────────────
                        // Kalkulahin ang row para sa signature (siguraduhing lampas sa Row 20)
                        int sigRow = startRow + dgvReports.Rows.Count + 8;
                        if (sigRow < 23) sigRow = 23;

                        // I-merge ang Columns 1 hanggang 3 para sa "Generated by"
                        var mergedRange = worksheet.Range(sigRow, 1, sigRow, 3);
                        mergedRange.Merge();
                        worksheet.Cell(sigRow, 1).Value = "Generated by: " + currentLoggedUser;
                        mergedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                        // Isulat ang Date Time sa Column 5 (E)
                        worksheet.Cell(sigRow, 4).Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");

                        // ── HETO ANG MAKAPANGYARIHANG LINYA: ─────────────────────────────────
                        // Awtomatikong luluwagan ang columns 1 hanggang kung ilang columns man ang meron ka
                        worksheet.Columns(1, dgvReports.Columns.Count).AdjustToContents();
                    }
                    else
                    {
                        // No template — plain export
                        workbook = new XLWorkbook();
                        worksheet = workbook.AddWorksheet(cmbReportType.Text);

                        // Header row
                        for (int j = 0; j < dgvReports.Columns.Count; j++)
                        {
                            var hCell = worksheet.Cell(1, j + 1);
                            hCell.Value = dgvReports.Columns[j].HeaderText;
                            hCell.Style.Font.Bold = true;
                            hCell.Style.Fill.BackgroundColor = XLColor.FromArgb(37, 99, 235);
                            hCell.Style.Font.FontColor = XLColor.White;
                        }

                        // Data rows
                        for (int i = 0; i < dgvReports.Rows.Count; i++)
                            for (int j = 0; j < dgvReports.Columns.Count; j++)
                                worksheet.Cell(i + 2, j + 1).Value =
                                    dgvReports.Rows[i].Cells[j].Value?.ToString() ?? "";

                        // Summary rows
                        int sumRow = dgvReports.Rows.Count + 3;
                        worksheet.Cell(sumRow, 1).Value = lblSumA.Text;
                        worksheet.Cell(sumRow + 1, 1).Value = lblSumB.Text;
                        worksheet.Cell(sumRow + 2, 1).Value = lblSumC.Text;

                        // Signature
                        int sigRow = sumRow + 4;
                        worksheet.Cell(sigRow, 1).Value = "Generated by: " + currentLoggedUser;
                        worksheet.Cell(sigRow, 2).Value = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();
                    }

                    // Save dialog
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                        sfd.FileName = cmbReportType.Text + "_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmm");
                        sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            workbook.SaveAs(sfd.FileName);
                            MessageBox.Show("Report exported successfully!\n" + sfd.FileName,
                                "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }

                    workbook.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Export Error: " + ex.Message, "Export Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlContentArea.Controls.AddRange(new Control[]
            {
        title, btnBack, filterBar, cardSum, dgvReports, lblStatus
            });
        }


        // ══════════════════════════════════════════════════════════════════════
        // ABOUT & USER MANAGEMENT
        // ══════════════════════════════════════════════════════════════════════
        private void LoadAbout()
        {
            if (pnlContentArea == null) return;
            pnlContentArea.Controls.Clear();

            Button btnBack = CreateBackButton();
            Label title = new Label { Text = "About MediFlow", Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(115, 15), AutoSize = true };

            Panel card = new Panel { BackColor = Color.White, Location = new Point(20, 70), Size = new Size(480, 260) };
            ApplyRoundedCorners(card, 12);
            card.Controls.Add(new Label { Text = "✚ MediFlow CMS Professional", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = clrMainBlue, Location = new Point(20, 20), AutoSize = true });
            card.Controls.Add(new Label { Text = "Version 1.0.0", ForeColor = clrTextGray, Location = new Point(20, 60), AutoSize = true });
            card.Controls.Add(new Label { Text = "Built with .NET WinForms & MySQL", Location = new Point(20, 88), AutoSize = true });
            card.Controls.Add(new Label { Text = "Roles: Admin · Doctor · Staff", Location = new Point(20, 116), AutoSize = true });
            card.Controls.Add(new Label { Text = $"Logged in as: {currentLoggedUser}  ({currentLoggedRole})", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, 155), AutoSize = true });

            pnlContentArea.Controls.AddRange(new Control[] { title, btnBack, card });
        }

        private void LoadUserManagement()
        {
            if (pnlContentArea == null) return;
            pnlContentArea.Controls.Clear();

            Button btnBack = CreateBackButton();
            Label title = new Label { Text = "Account Administration", Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(115, 15), AutoSize = true };

            Panel searchBar = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20, 58),
                Size = new Size(380, 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            searchBar.Resize += (s, e) => ApplyRoundedCorners(searchBar, 10);

            Label lblSearch = new Label { Text = "🔍  Search", ForeColor = clrTextGray, Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(14, 7), AutoSize = true };
            var txtSearch = new TextBox
            {
                PlaceholderText = "Username or full name...",
                Location = new Point(14, 26),
                Width = 340,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };
            searchBar.Controls.AddRange(new Control[] { lblSearch, txtSearch });

            dgvUsers = new DataGridView
            {
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                ReadOnly = true,
                EnableHeadersVisualStyles = false,
                Location = new Point(20, 120),
                Size = new Size(pnlContentArea.Width - 40, 235),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = clrBg;
            dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            Panel ctrlCard = CreateCard(pnlContentArea.Width - 40, 155);
            ctrlCard.Location = new Point(20, 365);
            ctrlCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            TextBox txtU = new TextBox { PlaceholderText = "Username", Location = new Point(20, 20), Width = 165 };
            TextBox txtP = new TextBox { PlaceholderText = "Password", Location = new Point(195, 20), Width = 165, UseSystemPasswordChar = true };
            TextBox txtN = new TextBox { PlaceholderText = "Full Name", Location = new Point(370, 20), Width = 195 };
            TextBox txtE = new TextBox { PlaceholderText = "Email", Location = new Point(575, 20), Width = 195 };
            ComboBox cR = new ComboBox { Location = new Point(780, 20), Width = 105, DropDownStyle = ComboBoxStyle.DropDownList };
            cR.Items.AddRange(new string[] { "Admin", "Doctor", "Staff" }); cR.SelectedIndex = 2;
            ComboBox cS = new ComboBox { Location = new Point(895, 20), Width = 105, DropDownStyle = ComboBoxStyle.DropDownList };
            cS.Items.AddRange(new string[] { "Active", "Inactive" }); cS.SelectedIndex = 0;

            Button bAdd = new Button { Text = "Add", Location = new Point(20, 75), Width = 110, Height = 38 }; StyleButton(bAdd);
            Button bUpd = new Button { Text = "Update", Location = new Point(140, 75), Width = 110, Height = 38 }; StyleButton(bUpd);
            Button bClr = new Button { Text = "Clear", Location = new Point(260, 75), Width = 95, Height = 38 }; StyleButton(bClr);
            Button bTog = new Button { Text = "Toggle Status", Location = new Point(365, 75), Width = 140, Height = 38, BackColor = clrDarkBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            ctrlCard.Controls.AddRange(new Control[] { txtU, txtP, txtN, txtE, cR, cS, bAdd, bUpd, bClr, bTog });

            Action clr = () => { txtU.Clear(); txtP.Clear(); txtN.Clear(); txtE.Clear(); cR.SelectedIndex = 2; cS.SelectedIndex = 0; dgvUsers?.ClearSelection(); };

            Action<string> fetch = (filter) =>
            {
                dgvUsers.Rows.Clear(); dgvUsers.Columns.Clear();
                dgvUsers.Columns.Add("id", "ID"); dgvUsers.Columns.Add("user", "Username");
                dgvUsers.Columns.Add("name", "Full Name"); dgvUsers.Columns.Add("email", "Email");
                dgvUsers.Columns.Add("role", "Role"); dgvUsers.Columns.Add("status", "Status");
                try
                {
                    using (MySqlConnection conn = DBConnection.GetConnection())
                    {
                        string q = "SELECT user_id,username,full_name,email,role,status FROM users" +
                                   (string.IsNullOrEmpty(filter) ? "" : " WHERE username LIKE @f OR full_name LIKE @f");
                        var cmd = new MySqlCommand(q, conn);
                        if (!string.IsNullOrEmpty(filter)) cmd.Parameters.AddWithValue("@f", "%" + filter + "%");
                        using (var dr = cmd.ExecuteReader())
                            while (dr.Read())
                                dgvUsers.Rows.Add(dr["user_id"], dr["username"], dr["full_name"], dr["email"], dr["role"], dr["status"]);
                    }
                }
                catch (Exception ex) { MessageBox.Show("DB error: " + ex.Message); }
            };

            fetch("");
            txtSearch.TextChanged += (s, e) => fetch(txtSearch.Text.Trim());
            dgvUsers.SelectionChanged += (s, e) =>
            {
                if (dgvUsers.SelectedRows.Count > 0)
                {
                    var r = dgvUsers.SelectedRows[0];
                    txtU.Text = r.Cells["user"].Value?.ToString() ?? "";
                    txtN.Text = r.Cells["name"].Value?.ToString() ?? "";
                    txtE.Text = r.Cells["email"].Value?.ToString() ?? "";
                    cR.Text = r.Cells["role"].Value?.ToString() ?? "Staff";
                    cS.Text = r.Cells["status"].Value?.ToString() ?? "Active";
                    txtP.Text = "";
                }
            };

            bClr.Click += (s, e) => clr();

            bAdd.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtU.Text) || string.IsNullOrEmpty(txtP.Text)) { MessageBox.Show("Username & Password required."); return; }
                try
                {
                    using (MySqlConnection conn = DBConnection.GetConnection())
                    {
                        var cmd = new MySqlCommand(
                            "INSERT INTO users(username,password,full_name,email,role,status)VALUES(@u,@p,@n,@e,@r,@s)", conn);
                        cmd.Parameters.AddWithValue("@u", txtU.Text.Trim());
                        // FIX #5: Hash password before inserting
                        cmd.Parameters.AddWithValue("@p", HashPassword(txtP.Text.Trim()));
                        cmd.Parameters.AddWithValue("@n", txtN.Text.Trim());
                        cmd.Parameters.AddWithValue("@e", txtE.Text.Trim());
                        cmd.Parameters.AddWithValue("@r", cR.Text);
                        cmd.Parameters.AddWithValue("@s", cS.Text);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Account created."); clr(); fetch("");
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };

            bUpd.Click += (s, e) =>
            {
                if (dgvUsers.SelectedRows.Count == 0) { MessageBox.Show("Select a row."); return; }
                string? id = dgvUsers.SelectedRows[0].Cells["id"].Value?.ToString();
                if (string.IsNullOrEmpty(id)) return;
                try
                {
                    using (MySqlConnection conn = DBConnection.GetConnection())
                    {
                        string q = "UPDATE users SET username=@u,full_name=@n,email=@e,role=@r,status=@s"
                                 + (string.IsNullOrEmpty(txtP.Text) ? "" : ",password=@p")
                                 + " WHERE user_id=@id";
                        var cmd = new MySqlCommand(q, conn);
                        cmd.Parameters.AddWithValue("@u", txtU.Text.Trim());
                        cmd.Parameters.AddWithValue("@n", txtN.Text.Trim());
                        cmd.Parameters.AddWithValue("@e", txtE.Text.Trim());
                        cmd.Parameters.AddWithValue("@r", cR.Text);
                        cmd.Parameters.AddWithValue("@s", cS.Text);
                        cmd.Parameters.AddWithValue("@id", id);
                        // FIX #5: Hash password on update too
                        if (!string.IsNullOrEmpty(txtP.Text)) cmd.Parameters.AddWithValue("@p", HashPassword(txtP.Text.Trim()));
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Updated."); clr(); fetch("");
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };

            bTog.Click += (s, e) =>
            {
                if (dgvUsers.SelectedRows.Count == 0) { MessageBox.Show("Select a row."); return; }
                string? id = dgvUsers.SelectedRows[0].Cells["id"].Value?.ToString();
                string? cur = dgvUsers.SelectedRows[0].Cells["status"].Value?.ToString();
                if (string.IsNullOrEmpty(id)) return;
                string ns = cur == "Active" ? "Inactive" : "Active";
                try
                {
                    using (MySqlConnection conn = DBConnection.GetConnection())
                    {
                        var cmd = new MySqlCommand("UPDATE users SET status=@s WHERE user_id=@id", conn);
                        cmd.Parameters.AddWithValue("@s", ns);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show($"Status set to {ns}."); clr(); fetch("");
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };

            pnlContentArea.Controls.AddRange(new Control[] { title, btnBack, searchBar, dgvUsers, ctrlCard });
        }
    }
}