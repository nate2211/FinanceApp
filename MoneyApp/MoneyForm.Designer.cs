using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using MoneyApp;
using Newtonsoft.Json.Linq;

namespace FinanceApp
{
    public partial class FinanceForm : Form
    {
        private Label profitLabel;
        private Label costLabel;
        private Label resultLabel;
        private TextBox profitTextBox;
        private TextBox costTextBox;
        private TextBox profitDescriptionTextBox;
        private TextBox costDescriptionTextBox;
        private Button calculateButton;
        private Button postProfitButton;
        private Button postCostButton;
        private Button removePostButton;
        private Button openCryptoFormButton;
        private ListBox displayListBox;

        private const string DataFile = "finance_data.txt";
        private List<string> records = new List<string>();

        public FinanceForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Finance Calculator";
            this.Width = 500;
            this.Height = 450;

            profitLabel = new Label() { Text = "Profit:", Left = 10, Top = 20, Width = 80 };
            costLabel = new Label() { Text = "Cost:", Left = 10, Top = 70, Width = 80 };
            resultLabel = new Label() { Text = "Final Amount: $0.00", Left = 10, Top = 320, Width = 250 };

            profitTextBox = new TextBox() { Left = 100, Top = 20, Width = 100 };
            profitDescriptionTextBox = new TextBox() { Left = 210, Top = 20, Width = 200, PlaceholderText = "Description" };
            costTextBox = new TextBox() { Left = 100, Top = 70, Width = 100 };
            costDescriptionTextBox = new TextBox() { Left = 210, Top = 70, Width = 200, PlaceholderText = "Description" };

            postProfitButton = new Button() { Text = "Post Profit", Left = 420, Top = 20, Width = 100 };
            postProfitButton.Click += PostProfitButton_Click;

            postCostButton = new Button() { Text = "Post Cost", Left = 420, Top = 70, Width = 100 };
            postCostButton.Click += PostCostButton_Click;

            removePostButton = new Button() { Text = "Remove Post", Left = 100, Top = 120, Width = 150 };
            removePostButton.Click += RemovePostButton_Click;

            calculateButton = new Button() { Text = "Calculate", Left = 100, Top = 150, Width = 100 };
            calculateButton.Click += CalculateButton_Click;

            openCryptoFormButton = new Button() { Text = "Crypto Prices", Left = 100, Top = 350, Width = 150 };
            openCryptoFormButton.Click += OpenCryptoFormButton_Click;

            displayListBox = new ListBox() { Left = 10, Top = 180, Width = 480, Height = 120 };

            Button openStockFormButton = new Button() { Text = "Stock Prices", Left = 250, Top = 350, Width = 150 };
            openStockFormButton.Click += OpenStockFormButton_Click;

            this.Controls.Add(openStockFormButton);
            this.Controls.Add(profitLabel);
            this.Controls.Add(costLabel);
            this.Controls.Add(resultLabel);
            this.Controls.Add(profitTextBox);
            this.Controls.Add(profitDescriptionTextBox);
            this.Controls.Add(costTextBox);
            this.Controls.Add(costDescriptionTextBox);
            this.Controls.Add(postProfitButton);
            this.Controls.Add(postCostButton);
            this.Controls.Add(removePostButton);
            this.Controls.Add(calculateButton);
            this.Controls.Add(displayListBox);
            this.Controls.Add(openCryptoFormButton);

            this.ResumeLayout(false);
        }
        private void OpenStockFormButton_Click(object sender, EventArgs e)
        {
            StockForm stockForm = new StockForm();
            stockForm.Show();
            this.Hide();
        }
        private void LoadData()
        {
            if (File.Exists(DataFile))
            {
                records.AddRange(File.ReadAllLines(DataFile));
                displayListBox.Items.AddRange(records.ToArray());
            }
        }

        private void SaveData()
        {
            File.WriteAllLines(DataFile, records);
        }

        private void PostProfitButton_Click(object sender, EventArgs e)
        {
            if (decimal.TryParse(profitTextBox.Text, out decimal profit))
            {
                string record = $"Profit: ${profit:F2} - {profitDescriptionTextBox.Text}";
                records.Add(record);
                displayListBox.Items.Add(record);
                SaveData();
            }
        }

        private void PostCostButton_Click(object sender, EventArgs e)
        {
            if (decimal.TryParse(costTextBox.Text, out decimal cost))
            {
                string record = $"Cost: ${cost:F2} - {costDescriptionTextBox.Text}";
                records.Add(record);
                displayListBox.Items.Add(record);
                SaveData();
            }
        }

        private void RemovePostButton_Click(object sender, EventArgs e)
        {
            if (displayListBox.SelectedItem != null)
            {
                string selectedItem = displayListBox.SelectedItem.ToString();
                records.Remove(selectedItem);
                displayListBox.Items.Remove(selectedItem);
                SaveData();
            }
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            decimal totalProfit = 0;
            decimal totalCost = 0;

            foreach (string record in records)
            {
                if (record.StartsWith("Profit: "))
                {
                    if (decimal.TryParse(record.Split('$')[1].Split(' ')[0], out decimal profit))
                    {
                        totalProfit += profit;
                    }
                }
                else if (record.StartsWith("Cost: "))
                {
                    if (decimal.TryParse(record.Split('$')[1].Split(' ')[0], out decimal cost))
                    {
                        totalCost += cost;
                    }
                }
            }

            decimal finalAmount = totalProfit - totalCost;
            resultLabel.Text = $"Final Amount: ${finalAmount:F2}";
        }
        private void OpenCryptoFormButton_Click(object sender, EventArgs e)
        {
            CryptoForm cryptoForm = new CryptoForm();
            cryptoForm.Show();
            this.Hide();
        }
    }
}

