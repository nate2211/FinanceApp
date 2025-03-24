using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tensorflow;
using Tensorflow.NumPy;
using Tensorflow.Keras.Engine;
using Tensorflow.Keras.Models;
using Tensorflow.Keras.Layers;
using Tensorflow.Keras.Optimizers;
using static Tensorflow.Binding;
using static Tensorflow.KerasApi;

public class Prediction
{
    private float minPrice;
    private float maxPrice;
    private Sequential model;
    private int windowSize;

    /// <summary>Load data from file, normalize it, and prepare training sequences.</summary>
    public (NDArray X, NDArray Y) LoadData(string filePath, int windowSize)
    {
        var priceList = new List<float>();

        foreach (string line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Split the line by dashes and spaces to isolate the price
            string[] parts = line.Split('-');

            // The price is usually the second component, which has "$" at the start
            string priceWithDollar = parts[1].Trim();

            // Remove the dollar sign and parse as a float
            if (float.TryParse(priceWithDollar.Replace("$", "").Trim(), out float price))
            {
                priceList.Add(price);
            }
        }

        if (priceList.Count == 0)
            throw new Exception("No price data found in file. Check file format.");

        // Normalize the prices using min-max scaling
        minPrice = priceList.Min();
        maxPrice = priceList.Max();

        for (int i = 0; i < priceList.Count; i++)
        {
            priceList[i] = (priceList[i] - minPrice) / (maxPrice - minPrice);
        }

        // Create sliding window sequences
        var X_list = new List<float[]>();
        var Y_list = new List<float>();
        int totalCount = priceList.Count;

        for (int i = 0; i < totalCount - windowSize; i++)
        {
            // Create a window of `windowSize` elements and the corresponding next value
            float[] window = priceList.GetRange(i, windowSize).ToArray();
            float nextValue = priceList[i + windowSize];
            X_list.Add(window);
            Y_list.Add(nextValue);
        }

        // Convert to NDArray
        float[,] XArray = new float[X_list.Count, windowSize];
        for (int i = 0; i < X_list.Count; i++)
        {
            for (int j = 0; j < windowSize; j++)
            {
                XArray[i, j] = X_list[i][j];
            }
        }
        NDArray X = np.array(XArray);
        NDArray Y = np.array(Y_list.ToArray());

        return (X, Y);
    }
    /// <summary>Builds a Sequential model for prediction.</summary>
    public void BuildModel(int windowSize)
    {
        this.windowSize = windowSize;
        model = keras.Sequential();
        model.add(keras.layers.Dense(64, activation: "relu", input_shape: new Shape(windowSize)));
        model.add(keras.layers.Dense(128));
        model.compile(optimizer: keras.optimizers.Adam(learning_rate: 0.001f),
                      loss: keras.losses.MeanSquaredError());
    }

    /// <summary>Train the model on the given data.</summary>
    public void Train(string filePath, int windowSize, int epochs = 50)
    {
        var (trainX, trainY) = LoadData(filePath, windowSize);
        BuildModel(windowSize);
        model.fit(trainX, trainY, batch_size: 16, epochs: epochs, verbose: 1);
    }

    /// <summary>Predict the next value given the recent window of data.</summary>
    /// <summary>Predict the next value given the recent window of data.</summary>
    public float PredictNext(float[] recentWindow)
    {
        if (model == null)
            throw new InvalidOperationException("Model is not trained yet.");

        if (recentWindow.Length != windowSize)
        {
            // Handle the case where the input length doesn't match the expected windowSize.
            // For example, if recentWindow is shorter, you might pad it with zeros:
            if (recentWindow.Length < windowSize)
            {
                float[] paddedWindow = new float[windowSize];
                Array.Copy(recentWindow, paddedWindow, recentWindow.Length);
                for (int i = recentWindow.Length; i < windowSize; i++)
                {
                    paddedWindow[i] = 0.0f; // or use a meaningful value for padding
                }
                recentWindow = paddedWindow;
            }
            // If recentWindow is longer, truncate it:
            else if (recentWindow.Length > windowSize)
            {
                float[] truncatedWindow = new float[windowSize];
                Array.Copy(recentWindow, truncatedWindow, windowSize);
                recentWindow = truncatedWindow;
            }
        }

        // Normalize the input window using the same min/max as training data
        float[] normWindow = new float[recentWindow.Length];
        for (int i = 0; i < recentWindow.Length; i++)
        {
            normWindow[i] = (recentWindow[i] - minPrice) / (maxPrice - minPrice);
        }

        // Prepare the input tensor for prediction
        var x = tf.constant(normWindow, shape: new Shape(1, recentWindow.Length));

        // Predict the next value
        var yPred = model.predict(x);
        float predNorm = yPred[0].ToArray<float>()[0];

        // De-normalize the predicted value to get the actual scale
        float predActual = predNorm * (maxPrice - minPrice) + minPrice;
        return predActual;
    }
}
