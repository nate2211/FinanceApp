using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FinanceApp;
using Newtonsoft.Json.Linq;

namespace MoneyApp
{
    public partial class KrogerForm : Form
    {
        private TextBox searchTextBox;
        private TextBox zipCodeTextBox;
        private Button searchButton;
        private Button addItemButton;
        private Button removeItemButton;
        private Button backButton;
        private Label resultLabel;
        private ListBox productListBox;
        private ListBox groceryListBox;  // Displaying Grocery List

        private const string TokenUrl = "https://api-ce.kroger.com/v1/connect/oauth2/token";
        private const string LocationsUrl = "https://api-ce.kroger.com/v1/locations";
        private const string ProductsUrl = "https://api-ce.kroger.com/v1/products";
        private const string GroceryListFile = "grocery_list.txt";

        private const string ClientId = "foodplanner-a888f6e28200ba527821ea95395eaf1f2526828669264750744";
        private const string ClientSecret = "uiOvcuosGfrReUzbtfTtzZSv5QDVQlbF5Wvvwt55";

        private string _accessToken;

        public KrogerForm()
        {
            this.Text = "Kroger Products";
            this.Width = 600;
            this.Height = 700;

            Label zipLabel = new Label() { Text = "Zip Code:", Left = 10, Top = 20, Width = 60 };
            zipCodeTextBox = new TextBox() { Left = 80, Top = 20, Width = 100 };

            Label searchLabel = new Label() { Text = "Search for a product:", Left = 10, Top = 50, Width = 150 };
            searchTextBox = new TextBox() { Left = 160, Top = 50, Width = 200 };
            searchButton = new Button() { Text = "Search", Left = 370, Top = 50, Width = 80 };
            searchButton.Click += SearchButton_Click;

            addItemButton = new Button() { Text = "Add Item", Left = 10, Top = 400, Width = 100 };
            addItemButton.Click += AddItemButton_Click;

            removeItemButton = new Button() { Text = "Remove Item", Left = 120, Top = 400, Width = 100 };
            removeItemButton.Click += RemoveItemButton_Click;

            backButton = new Button() { Text = "Back", Left = 10, Top = 630, Width = 150 };
            backButton.Click += BackButton_Click;

            resultLabel = new Label() { Text = "Select a product to add to your grocery list", Left = 10, Top = 80, Width = 360 };
            productListBox = new ListBox() { Left = 10, Top = 110, Width = 550, Height = 150 };
            productListBox.HorizontalScrollbar = true;

            Label groceryListLabel = new Label() { Text = "Your Grocery List:", Left = 10, Top = 450, Width = 150 };
            groceryListBox = new ListBox() { Left = 10, Top = 470, Width = 550, Height = 150 };
            groceryListBox.HorizontalScrollbar = true;

            this.Controls.Add(zipLabel);
            this.Controls.Add(zipCodeTextBox);
            this.Controls.Add(searchLabel);
            this.Controls.Add(searchTextBox);
            this.Controls.Add(searchButton);
            this.Controls.Add(addItemButton);
            this.Controls.Add(removeItemButton);
            this.Controls.Add(backButton);
            this.Controls.Add(resultLabel);
            this.Controls.Add(productListBox);
            this.Controls.Add(groceryListLabel);
            this.Controls.Add(groceryListBox);

            LoadGroceryList();
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            productListBox.Items.Clear();
            string query = searchTextBox.Text.Trim();
            string zipCode = zipCodeTextBox.Text.Trim();

            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(zipCode))
            {
                MessageBox.Show("Please enter a product name and a zip code.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string token = await GetAccessTokenAsync();
                string locationId = await GetLocationIdAsync(token, zipCode);

                if (string.IsNullOrEmpty(locationId))
                {
                    resultLabel.Text = "No Kroger locations found for the given zip code.";
                    return;
                }

                await SearchProductsAsync(token, locationId, query);
            }
            catch (Exception ex)
            {
                resultLabel.Text = "Error retrieving products.";
                Console.WriteLine(ex.Message);
            }
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken)) return _accessToken;

            using (var client = new HttpClient())
            {
                var authString = $"{ClientId}:{ClientSecret}";
                var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("scope", "product.compact")
                });

                var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
                {
                    Content = requestContent
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                _accessToken = json["access_token"]?.ToString();
                return _accessToken;
            }
        }

        private async Task<string> GetLocationIdAsync(string token, string zipCode)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.GetAsync($"{LocationsUrl}?filter.zipCode.near={zipCode}");
                response.EnsureSuccessStatusCode();

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                return json["data"]?[0]?["locationId"]?.ToString();
            }
        }

        private async Task SearchProductsAsync(string token, string locationId, string query)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await client.GetAsync($"{ProductsUrl}?filter.locationId={locationId}&filter.term={query}");
                response.EnsureSuccessStatusCode();

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());

                foreach (var product in json["data"])
                {
                    string name = product["description"]?.ToString();
                    string price = product["items"]?[0]?["price"]?["regular"]?.ToString() ?? "Price not available";
                    productListBox.Items.Add($"{name} - ${price}");
                }
            }
        }

        private void AddItemButton_Click(object sender, EventArgs e)
        {
            if (productListBox.SelectedItem != null)
            {
                groceryListBox.Items.Add(productListBox.SelectedItem.ToString());
                SaveGroceryList();
            }
        }

        private void RemoveItemButton_Click(object sender, EventArgs e)
        {
            if (groceryListBox.SelectedItem != null)
            {
                groceryListBox.Items.Remove(groceryListBox.SelectedItem);
                SaveGroceryList();
            }
        }

        private void LoadGroceryList()
        {
            if (File.Exists(GroceryListFile))
            {
                var items = File.ReadAllLines(GroceryListFile);
                groceryListBox.Items.AddRange(items);
            }
        }

        private void SaveGroceryList()
        {
            File.WriteAllLines(GroceryListFile, groceryListBox.Items.Cast<string>());
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            FinanceForm financeForm = new FinanceForm();
            financeForm.Show();
            this.Hide();
        }
    }
}
