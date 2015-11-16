﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FT_Rider.Classes;
using FT_Rider.Resources;
using Telerik.Windows.Controls.PhoneTextBox;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace FT_Rider.Pages
{
    public partial class Login : PhoneApplicationPage
    {
        IsolatedStorageFile iSOFile = IsolatedStorageFile.GetUserStoreForApplication();
        List<UserData> objUserDataList = new List<UserData>();

        private bool Validate(string text)
        {
            //Your validation logic
            return false;
        }

        public class Data
        {
            public string Name { get; set; }
        }

        public Login()
        {
            InitializeComponent();
            this.rad_Account.DataContext = new Data { Name = "Email" };
            this.rad_Password.DataContext = new Data { Name = "Passsword" };
            this.Loaded += Login_Loaded;
        }


        private void Login_Loaded(object sender, RoutedEventArgs e)
        {
            var Settings = IsolatedStorageSettings.ApplicationSettings;
            //Check if user already login,so we need to direclty navigate to details page instead of showing login page when user launch the app.  
            if (Settings.Contains("CheckLogin"))
            {
                NavigationService.Navigate(new Uri("RiderProfile.xaml", UriKind.Relative));
            }
            else
            {
                if (iSOFile.FileExists("RegistrationDetails"))//loaded previous items into list  
                {
                    using (IsolatedStorageFileStream fileStream = iSOFile.OpenFile("RegistrationDetails", FileMode.Open))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(typeof(List<UserData>));
                        objUserDataList = (List<UserData>)serializer.ReadObject(fileStream);

                    }
                }
            }
        }



        private void tbn_Tap_Login(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //if (rad_Account.Text != "" && rad_Password.ToString() != "")
            //{
            //    int Temp = 0;
            //    foreach (var UserLogin in objUserDataList)
            //    {
            //        if (rad_Account.Text == UserLogin.Email && rad_Password.ToString() == UserLogin.Password)
            //        {
            //            Temp = 1;
            //            var Settings = IsolatedStorageSettings.ApplicationSettings;
            //            Settings["CheckLogin"] = StaticVariables.strLoginSucess;//write iso    

            //            if (iSOFile.FileExists("CurrentLoginUserDetails"))
            //            {
            //                iSOFile.DeleteFile("CurrentLoginUserDetails");
            //            }
            //            using (IsolatedStorageFileStream fileStream = iSOFile.OpenFile("CurrentLoginUserDetails", FileMode.Create))
            //            {
            //                DataContractSerializer serializer = new DataContractSerializer(typeof(UserData));

            //                serializer.WriteObject(fileStream, UserLogin);

            //            }
            //            NavigationService.Navigate(new Uri("/Pages/HomePage.xaml", UriKind.Relative));
            //        }
            //    }
            //    if (Temp == 0)
            //    {
            //        rad_Password.ChangeValidationState(ValidationState.Invalid, "");
            //        rad_Account.ChangeValidationState(ValidationState.Invalid, "");
            //    }
            //}
            //else
            //{
            //    rad_Password.ChangeValidationState(ValidationState.Invalid, "");
            //    rad_Account.ChangeValidationState(ValidationState.Invalid, "");
            //}

        }

        private void tbn_Tap_Register(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/RiderRegister.xaml", UriKind.Relative));
        }

        private void tbl_Tap_LostPassword(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/RiderLostPassword.xaml", UriKind.Relative));
        }


        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/HomePage.xaml", UriKind.Relative));
        }

        string URL = "http://123.30.236.109:8088/TN/restServices/RiderController/LoginRider?json={'uid':'apl.ytb2@gmail.com','pw':'Abc123!','mid':'','mType':'AND'}";

        void sendRequest()
        {
           Uri myUri = new Uri(http://www.yourwebsite.com);
           HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);
           myR1equest.Method = AppResources.POST;
           myRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), myRequest);
        }

        void GetRequestStreamCallback(IAsyncResult callbackResult)
        {
            HttpWebRequest myRequest = (HttpWebRequest)callbackResult.AsyncState;

            // End the stream request operation
            Stream postStream = myRequest.EndGetRequestStream(callbackResult);

            // Create the post data
            string postData = "INSERT HERE THE JASON YOU WANT TO SEND";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Add the post data to the web request
            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();

            // Start the web request
            myRequest.BeginGetResponse(new AsyncCallback(GetResponsetStreamCallback), myRequest);
        }

        void GetResponsetStreamCallback(IAsyncResult callbackResult)
        {
            lib = new ApiLibrary();

            try
            {
                HttpWebRequest request = (HttpWebRequest)callbackResult.AsyncState;
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(callbackResult);
                string result = "";
                using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream()))
                {
                    result = httpWebStreamReader.ReadToEnd();
                }

                string APIResult = result;

            }
            catch (Exception e)
            {

            }
        }
    }
}