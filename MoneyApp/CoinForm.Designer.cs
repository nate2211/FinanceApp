﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using FinanceApp;
using Newtonsoft.Json.Linq;

namespace MoneyApp
{
    public class CryptoForm : Form
    {
        private TextBox coinTextBox;
        private Button queryButton;
        private Button postPriceButton;
        private Button removePriceButton;
        private Button backButton;
        private Button predictButton;
        private Label resultLabel;
        private ListBox priceListBox;
        private const string CryptoDataFile = "crypto_data.txt";
        private Prediction prediction;

        public CryptoForm()
        {
            prediction = new Prediction();
            prediction.Train(CryptoDataFile, windowSize: 10, epochs: 15);
            this.Text = "Crypto Prices";
            this.Width = 400;
            this.Height = 450;

            Label coinLabel = new Label() { Text = "Enter Coin:", Left = 10, Top = 20, Width = 100 };
            coinTextBox = new TextBox() { Left = 120, Top = 20, Width = 150 };
            queryButton = new Button() { Text = "Query", Left = 120, Top = 50, Width = 100 };
            queryButton.Click += QueryButton_Click;

            postPriceButton = new Button() { Text = "Post Price", Left = 120, Top = 80, Width = 100 };
            postPriceButton.Click += PostPriceButton_Click;

            removePriceButton = new Button() { Text = "Remove Price", Left = 10, Top = 80, Width = 100 };
            removePriceButton.Click += RemovePriceButton_Click;

            backButton = new Button() { Text = "Back to Finance", Left = 10, Top = 320, Width = 150 };
            backButton.Click += BackButton_Click;

            predictButton = new Button() { Text = "Predict", Left = 250, Top = 80, Width = 100 };
            predictButton.Click += PredictButton_Click;

            resultLabel = new Label() { Text = "Price: $0.00", Left = 10, Top = 110, Width = 300 };
            priceListBox = new ListBox() { Left = 10, Top = 140, Width = 360, Height = 150 };
            LoadCryptoData();

            this.Controls.Add(coinLabel);
            this.Controls.Add(coinTextBox);
            this.Controls.Add(queryButton);
            this.Controls.Add(postPriceButton);
            this.Controls.Add(removePriceButton);
            this.Controls.Add(backButton);
            this.Controls.Add(predictButton);
            this.Controls.Add(resultLabel);
            this.Controls.Add(priceListBox);
        }

        private void LoadCryptoData()
        {
            if (File.Exists(CryptoDataFile))
            {
                priceListBox.Items.AddRange(File.ReadAllLines(CryptoDataFile));
            }
        }

        private void PostPriceButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(resultLabel.Text) && resultLabel.Text.StartsWith("Price: "))
            {
                string priceText = resultLabel.Text.Replace("Price: ", "");
                string record = $"{coinTextBox.Text.ToUpper()} - {priceText} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                priceListBox.Items.Add(record);
                File.WriteAllLines(CryptoDataFile, priceListBox.Items.Cast<string>());
            }
        }

        private async void QueryButton_Click(object sender, EventArgs e)
        {
            string coin = coinTextBox.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(coin)) return;

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"https://api.coinbase.com/v2/prices/{coin}-USD/spot";
                    string json = await client.GetStringAsync(url);
                    JObject data = JObject.Parse(json);
                    string price = data["data"]["amount"].ToString();
                    resultLabel.Text = $"Price: ${price}";
                }
                catch
                {
                    resultLabel.Text = "Invalid coin or network error";
                }
            }
        }

        private void PredictButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(coinTextBox.Text) && decimal.TryParse(resultLabel.Text.Replace("Price: $", ""), out decimal price))
            {
                float[] recentData = { (float)price }; // recent prices
                float predictedValue = prediction.PredictNext(recentData);
      

                string predictionRecord = $"Prediction - {coinTextBox.Text.ToUpper()} - Predicted Value: ${predictedValue:F2} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                priceListBox.Items.Add(predictionRecord);

                File.WriteAllLines(CryptoDataFile, priceListBox.Items.Cast<string>());
            }
            else
            {
                MessageBox.Show("Please enter a valid coin and perform a query first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                File.WriteAllLines(CryptoDataFile, priceListBox.Items.Cast<string>());
            }
        }
    }
}
