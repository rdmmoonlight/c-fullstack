using Android.App;
using Android.OS;
using Android.Widget;
using System.Net.Http;

namespace CFullstackAndroid
{
    [Activity(Label = "C-Fullstack App", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private static readonly HttpClient client = new HttpClient();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Button button = new Button(this) { Text = "Fetch Backend API" };
            TextView textView = new TextView(this) { Text = "Press button..." };

            LinearLayout layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            layout.AddView(button);
            layout.AddView(textView);
            SetContentView(layout);

            button.Click += async (sender, e) =>
            {
                try
                {
                    // Di emulator Android, localhost PC diakses lewat 10.0.2.2
                    string response = await client.GetStringAsync("http://10.0.2.2/api/");
                    textView.Text = response;
                }
                catch
                {
                    textView.Text = "Connection Failed";
                }
            };
        }
    }
}
