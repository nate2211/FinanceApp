using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using FinanceApp;
using Newtonsoft.Json.Linq;

namespace MoneyApp
{
    public class StockForm : Form
    {
        private TextBox stockTextBox;
        private Button queryButton;
        private Button postPriceButton;
        private Button removePriceButton;
        private Button backButton;
        private Button predictButton; // New button for making predictions
        private Label resultLabel;
        private ListBox priceListBox;
        private const string StockDataFile = "stock_data.txt";
        private const string ApiKey = "dU9vyfnfBxER77rEC5p7doaCBbHH_eyO";
        private Prediction prediction; // Prediction model instance

        public StockForm()
        {
            prediction = new Prediction();
            prediction.Train(StockDataFile, windowSize: 10, epochs: 15);

            this.Text = "Stock Prices";
            this.Width = 350;
            this.Height = 400;

            Label stockLabel = new Label() { Text = "Enter Stock:", Left = 10, Top = 20, Width = 100 };
            stockTextBox = new TextBox() { Left = 120, Top = 20, Width = 150 };
            queryButton = new Button() { Text = "Query", Left = 120, Top = 50, Width = 100 };
            queryButton.Click += QueryButton_Click;

            postPriceButton = new Button() { Text = "Post Price", Left = 120, Top = 80, Width = 100 };
            postPriceButton.Click += PostPriceButton_Click;

            removePriceButton = new Button() { Text = "Remove Price", Left = 10, Top = 80, Width = 100 };
            removePriceButton.Click += RemovePriceButton_Click;

            backButton = new Button() { Text = "Back to Finance", Left = 10, Top = 320, Width = 150 };
            backButton.Click += BackButton_Click;

            predictButton = new Button() { Text = "Predict", Left = 230, Top = 50, Width = 100 }; // New predict button
            predictButton.Click += PredictButton_Click;

            resultLabel = new Label() { Text = "Price: $0.00", Left = 10, Top = 110, Width = 300 };
            priceListBox = new ListBox() { Left = 10, Top = 140, Width = 310, Height = 150 };

            LoadStockData();

            this.Controls.Add(stockLabel);
            this.Controls.Add(stockTextBox);
            this.Controls.Add(queryButton);
            this.Controls.Add(postPriceButton);
            this.Controls.Add(removePriceButton);
            this.Controls.Add(backButton);
            this.Controls.Add(predictButton);
            this.Controls.Add(resultLabel);
            this.Controls.Add(priceListBox);
        }

        private void LoadStockData()
        {
            if (File.Exists(StockDataFile))
            {
                priceListBox.Items.AddRange(File.ReadAllLines(StockDataFile));
            }
        }

        private void PostPriceButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(resultLabel.Text) && resultLabel.Text.StartsWith("Previous Close: "))
            {
                string priceText = resultLabel.Text.Replace("Previous Close: $", "").Trim();
                string record = $"{stockTextBox.Text.ToUpper()} - ${priceText} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                priceListBox.Items.Add(record);
                File.WriteAllLines(StockDataFile, priceListBox.Items.Cast<string>());
            }
            else
            {
                MessageBox.Show("No valid stock price to post. Please query first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void QueryButton_Click(object sender, EventArgs e)
        {
            string stock = stockTextBox.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(stock)) return;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Alternative endpoint for free-tier users
                    string url = $"https://api.polygon.io/v2/aggs/ticker/{stock}/prev?apiKey={ApiKey}";
                    string json = await client.GetStringAsync(url);
                    JObject data = JObject.Parse(json);

                    if (data["results"] != null && data["results"].HasValues)
                    {
                        string price = data["results"][0]["c"]?.ToString(); // Extract the closing price
                        resultLabel.Text = $"Previous Close: ${price}";
                    }
                    else
                    {
                        resultLabel.Text = "Invalid stock ticker or no data available.";
                    }
                }
                catch (HttpRequestException ex)
                {
                    resultLabel.Text = "API request failed.";
                    Console.WriteLine($"Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    resultLabel.Text = "Network error or invalid API response.";
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void PredictButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(stockTextBox.Text) && decimal.TryParse(resultLabel.Text.Replace("Previous Close: $", ""), out decimal price))
            {
                // Use a simple recent window example (in a real scenario, you might gather multiple recent prices)
                float[] recentData = { (float)price };
                float predictedValue = prediction.PredictNext(recentData);

                // Display the predicted value
                string predictionRecord = $"Prediction - {stockTextBox.Text.ToUpper()} - Predicted Value: ${predictedValue:F2} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                priceListBox.Items.Add(predictionRecord);

                // Save the prediction to the file
                File.WriteAllLines(StockDataFile, priceListBox.Items.Cast<string>());
            }
            else
            {
                MessageBox.Show("Please enter a valid stock and perform a query first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            FinanceForm financeForm = new FinanceForm();
            financeForm.Show();
            this.Hide();
        }

        private void RemovePriceButton_Click(object sender, EventArgs e)
        {
            if (priceListBox.SelectedItem != null)
            {
                priceListBox.Items.Remove(priceListBox.SelectedItem);
                File.WriteAllLines(StockDataFile, priceListBox.Items.Cast<string>());
            }
        }
    }
}
