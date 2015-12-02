﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Device.Location;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.IO.IsolatedStorage;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Maps.Services;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Devices;
using Microsoft.Phone.Notification;
using Windows.Devices.Geolocation;
using Newtonsoft.Json;
using FT_Driver.Resources;
using FT_Driver.Classes;
using System.Text;
using System.Diagnostics;

namespace FT_Driver.Pages
{
    public partial class HomePage : PhoneApplicationPage
    {
        //USER DATA
        IsolatedStorageSettings tNetUserLoginData = IsolatedStorageSettings.ApplicationSettings;
        IsolatedStorageSettings tNetAppSetting = IsolatedStorageSettings.ApplicationSettings;
        DriverLogin userData = new DriverLogin();
        string userId = "";
        string pwmd5 = "";


        //For Store Points
        List<GeoCoordinate> driverCoordinates = new List<GeoCoordinate>();

        //For Router        
        GeocodeQuery driverGeocodeQuery = null;
        RouteQuery driverQuery = null;
        MapRoute driverMapRoute = null;
        Route driverRoute = null;

        //For get Current Location
        Geolocator driverFirstGeolocator = null;
        Geoposition driverFirstGeoposition = null;
        MapOverlay driverMapOverlay = null;
        MapLayer driverMapLayer = null;


        //VibrateController
        VibrateController vibrateController = VibrateController.Default;

        //For Distance
        Double distanceMeter;

        //Rider Destination Icon Overlay
        MapOverlay driverDestinationIconOverlay;

        //For Update Current Location
        double currentLat;
        double currentLng;
        int countForUpdateLocation = 0;

        //For process bar
        double tmpLat;
        double tmpLng;

        //For menu
        double initialPosition;
        bool _viewMoved = false;

        //For timer
        DispatcherTimer updateLocationTimer;

        //Fot Update Notification
        string pushChannelURI = string.Empty;
        string notificationReceivedString = string.Empty;
        string notificationType = string.Empty;

        public HomePage()
        {
            InitializeComponent();

            //Tạo kênh Notification
            CreatePushChannel();


            //get First Local Position
            GetCurrentCorrdinate();


            //Get User data from login
            if (tNetUserLoginData.Contains("UserLoginData"))
            {
                userData = (DriverLogin)tNetUserLoginData["UserLoginData"];
                userId = (string)tNetUserLoginData["UserId"];
                pwmd5 = (string)tNetUserLoginData["PasswordMd5"];
            }


            //Open Status Screen
            grv_ProcessScreen.Visibility = Visibility.Visible; //Enable Process bar


            this.LoadDriverProfile();
            this.UpdateDriverStatus(ConstantVariable.dStatusNotAvailable); //"NA"


            updateLocationTimer = new DispatcherTimer();
            updateLocationTimer.Tick += new EventHandler(updateLocationTimer_Tick);
            updateLocationTimer.Interval = new TimeSpan(0, 0, 0, 10); //Sau năm dây sẽ chạy cập nhật nếu như lần cập nhật trước không thành công    

            //Cập nhật tọa độ của lái xe lên server
            this.UpdateCurrentLocation(currentLat, currentLng);
        }

        private void updateLocationTimer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Chạy Update Location Timer"); //DELETE AFTER FINISH
            UpdateCurrentLocation(currentLat, currentLng);
            //throw new NotImplementedException();
        }



        /// <summary>
        /// Mỗi khi map thay đổi, hai biến currentLat và currentLng sẽ được cập nhật
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void map_DriverMap_CenterChanged(object sender, MapCenterChangedEventArgs e)
        {
            currentLat = map_DriverMap.Center.Latitude;
            currentLng = map_DriverMap.Center.Longitude;
        }

        private void map_DriverMap_ResolveCompleted(object sender, MapResolveCompletedEventArgs e)
        {
            Debug.WriteLine("DriverMap_ResolveCompleted"); //DELETE AFTER FINISH

            if (new GeoCoordinate(Math.Round(map_DriverMap.Center.Latitude, 5), Math.Round(map_DriverMap.Center.Longitude, 5)).Equals(new GeoCoordinate(tmpLat, tmpLng)))
            {
                grv_ProcessScreen.Visibility = Visibility.Collapsed; //Disable process bar
            }
        }



        /// <summary>
        /// Display Status
        /// </summary>
        private void isGridAcceptRejectOn()
        {
            grv_AcceptReject.Visibility = Visibility.Visible;
            btn_ChangeStatus.Visibility = Visibility.Collapsed;
        }



        //------ BEGIN Update Driver Status ------//
        private async void UpdateDriverStatus(string stt) //Chưa check try catch
        {
            //{"uid":"dao@gmail.com","pw":"b65bd772c3b0dfebf0a189efd420352d","status":"NA"}
            //Nếu đang hoạt động (AC), muốn chuyển qua chế độ nghỉ thì truyền vào "AC"
            //Nếu đang ở trạn thái nghỉ (NA) muốn chuyển qua chế độ hoạt động thì truyền vào "NA"
            //Nếu bắt đầu đón khách thì chuyển qua chế độ bận (BU)
            var uid = userId;
            var pw = pwmd5;
            var status = stt;
            var input = string.Format("{{\"uid\":\"{0}\",\"pw\":\"{1}\",\"status\":\"{2}\"}}", uid, pw, status);
            var output = await GetJsonFromPOSTMethod.GetJsonString(ConstantVariable.tNetDriverUpdateStatus, input);
            try
            {
                var driverUpdate = JsonConvert.DeserializeObject<BaseResponse>(output);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update Driver Status không thành công"); //DELETE AFTER FINISH
                //throw; //Exc here <<<<<<<<<<<<<<<<<<
            }

            Debug.WriteLine("Update Driver Status OK"); //DELETE AFTER FINISH
        }
        //------ END Update Driver Status ------//





        //------ BEGIN Update Current Location to Server ------//
        private async void UpdateCurrentLocation(double lat, double lng)
        {
            //{"uid":"dao@gmail.com","lat":"21.038993","lng":"105.799242"}
            if (Math.Round(lat, 0) != 0 && Math.Round(lng, 0) != 0)
            {
                //Chuyển qua replace
                //Chưa chuẩn
                var uid = userId;
                var input = string.Format("{{\"uid\":\"{0}\",\"lat\":\"{1}\",\"lng\":\"{2}\"}}", uid, lat.ToString().Replace(',', '.'), lng.ToString().Replace(',', '.'));
                try
                {
                    var output = await GetJsonFromPOSTMethod.GetJsonString(ConstantVariable.tNetDriverUpdateCurrentLocation, input);
                    if (output != null)
                    {
                        var driverUpdate = JsonConvert.DeserializeObject<BaseResponse>(output);
                        //Sau khi chạy OK sẽ tắt Thread
                        updateLocationTimer.Stop();
                        Debug.WriteLine("Cập nhật vị trí xe cho Driver OK");
                        MessageBox.Show("Cập nhật vị trí ve thành công");
                    }
                }
                catch (Exception)
                {
                    Debug.WriteLine("Update Location Không OK"); //DELETE AFTER FINISH
                    ///
                }
            }
            else
            {
                updateLocationTimer.Start();
                Debug.WriteLine("Không chạy được cập nhật vị trí xe");
            }

        }
        //------ END Update Current Location to Server ------//





        //------ BEGIN get driver profile ------//
        private void LoadDriverProfile()
        {
            tbl_FirstName.Text = userData.content.driverInfo.fName;
            tbl_LastName.Text = userData.content.driverInfo.lName;

            Debug.WriteLine("Load Driver Profile OK"); //DELETE AFTER FINISH
        }
        //------ END get driver profile ------//



        //------ BEGIN get current Position ------//
        private async void GetCurrentCorrdinate()
        {

            //get position
            driverFirstGeolocator = new Geolocator();
            driverFirstGeolocator.DesiredAccuracy = PositionAccuracy.High;
            driverFirstGeolocator.MovementThreshold = 20;
            driverFirstGeolocator.ReportInterval = 100;
            driverFirstGeoposition = await driverFirstGeolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));


            //Add img_CurrentLocation to Map
            Image currentLocationPin = new Image();
            currentLocationPin.Source = new BitmapImage(new Uri("/Images/Icons/img_CurrentLocation.png", UriKind.Relative));
            currentLocationPin.Height = 27;
            currentLocationPin.Width = 25;

            driverMapOverlay = new MapOverlay();
            driverMapOverlay.Content = currentLocationPin; //Phải khai báo 1 lớp Overlay vì Overlay có thuộc tính tọa độ (GeoCoordinate)
            driverMapOverlay.GeoCoordinate = new GeoCoordinate(driverFirstGeoposition.Coordinate.Latitude, driverFirstGeoposition.Coordinate.Longitude);
            driverMapOverlay.PositionOrigin = new Point(0.5, 0.5);

            driverMapLayer = new MapLayer();
            driverMapLayer.Add(driverMapOverlay); //Phải khai báo 1 Layer vì không thể add trực tiếp Overlay vào Map, mà phải thông qua Layer của Map
            map_DriverMap.Layers.Add(driverMapLayer);

            //// initialize pickup coordinates
            tmpLat = Math.Round(driverFirstGeoposition.Coordinate.Latitude, 5);
            tmpLng = Math.Round(driverFirstGeoposition.Coordinate.Longitude, 5);

            driverFirstGeolocator.PositionChanged += geolocator_PositionChanged;

            //Set Center view
            map_DriverMap.SetView(driverFirstGeoposition.Coordinate.ToGeoCoordinate(), 16, MapAnimationKind.Linear);

            Debug.WriteLine("Get Current Coordinate OK"); //DELETE AFTER FINISH

        }

        private void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Debug.WriteLine("Thay đổi vị trí map"); //DELETE AFTER FINISH
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {

                Geocoordinate geocoordinate = geocoordinate = args.Position.Coordinate;
                driverMapOverlay.GeoCoordinate = geocoordinate.ToGeoCoordinate(); //Cứ mỗi lần thay đổi vị trí, Map sẽ cập nhật tọa độ của Marker

                //For Update Current Location
                //Cứ sau 5 lần thay đổi vị trí trên Map, thì sẽ cập nhật vị trí lên server một lần
                //Việc này để giảm req lên sv và tránh hao pin cho tiết bị
                countForUpdateLocation++;
                if (countForUpdateLocation == 4)
                {
                    Debug.WriteLine("Cập nhật vị trí Driver lên Server"); //DELETE AFTER FINISH
                    UpdateCurrentLocation(geocoordinate.Latitude, geocoordinate.Longitude);
                    countForUpdateLocation = 0;
                }
            });

        }
        //------ END get current Position ------//






        //------ BEGIN route Direction on Map ------//
        private async void getMapRouteTo(double lat, double lng)
        {
            //driverCoordinates.RemoveAll(item => item == null);
            //Delete Previous Route if exist
            if (driverMapRoute != null)
            {
                //delete route
                map_DriverMap.RemoveRoute(driverMapRoute);
                driverMapRoute = null;
                driverQuery = null;
                driverMapLayer.Remove(driverDestinationIconOverlay);
            }

            Geolocator driverGeolocator = new Geolocator();
            driverGeolocator.DesiredAccuracyInMeters = 5;
            Geoposition driverGeoPosition = null;
            try
            {
                //Set Position point
                driverGeoPosition = await driverGeolocator.GetGeopositionAsync(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10));
                driverCoordinates.Add(new GeoCoordinate(driverGeoPosition.Coordinate.Latitude, driverGeoPosition.Coordinate.Longitude));
            }
            catch (UnauthorizedAccessException)
            {
                //Dịch vụ định vị đang tắt, vui lòng bật lên hoặc kiểm tra lại các thiết đặt.
                MessageBox.Show("(Mã lỗi 303) " + ConstantVariable.errServiceIsOff);
            }
            catch (Exception ex)
            {
                // Something else happened while acquiring the location.
                MessageBox.Show(ex.Message);
            }

            driverGeocodeQuery = new GeocodeQuery();
            driverGeocodeQuery.SearchTerm = lat.ToString().Replace(',', '.') + "," + lng.ToString().Replace(',', '.');
            driverGeocodeQuery.GeoCoordinate = new GeoCoordinate(driverGeoPosition.Coordinate.Latitude, driverGeoPosition.Coordinate.Longitude);


            driverGeocodeQuery.QueryCompleted += Mygeocodequery_QueryCompleted;
            driverGeocodeQuery.QueryAsync();
        }


        private void Mygeocodequery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Error == null)
            {
                try
                {
                    driverQuery = new RouteQuery();
                    driverCoordinates.Add(e.Result[0].GeoCoordinate);
                    driverQuery.Waypoints = driverCoordinates;
                    driverQuery.QueryCompleted += MyQuery_QueryCompleted;
                    driverQuery.QueryAsync();
                    driverGeocodeQuery.Dispose();
                }
                catch (Exception)
                {

                    MessageBox.Show("(Mã lỗi 304) " + ConstantVariable.errInvalidAddress);
                }
            }
        }



        private void MyQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {

            //if valid address input
            if (e.Error == null)
            {
                //if (driverMapRoute != null)
                //{
                //    map_DriverMap.RemoveRoute(driverMapRoute);
                //    driverMapLayer.Remove(driverDestinationIconOverlay);
                //    driverMapRoute = null;
                //}                
                driverRoute = e.Result;
                driverMapRoute = new MapRoute(driverRoute);
                //Makeup for router
                driverMapRoute.Color = Color.FromArgb(255, (byte)185, (byte)207, (byte)231); // aRGB for #b9cfe7
                map_DriverMap.AddRoute(driverMapRoute);
                driverQuery.Dispose();

                //get Coordinate of Destination Point
                double destinationLatitude = driverCoordinates[driverCoordinates.Count - 1].Latitude;
                double destinationLongtitude = driverCoordinates[driverCoordinates.Count - 1].Longitude;

                //Set Map Center
                this.map_DriverMap.Center = new GeoCoordinate(destinationLatitude - 0.001500, destinationLongtitude);

                // Create a small Point to mark the current location.
                Image myPositionIcon = new Image();
                myPositionIcon.Source = new BitmapImage(new Uri("/Images/Icons/img_DestinationPoint.png", UriKind.Relative));
                myPositionIcon.Height = 35;
                myPositionIcon.Width = 29;

                // Create a MapOverlay to contain the circle.
                driverDestinationIconOverlay = new MapOverlay();
                driverDestinationIconOverlay.Content = myPositionIcon;

                //MapOverlay PositionOrigin to 0.3, 0.9 MapOverlay will align it's center towards the GeoCoordinate
                driverDestinationIconOverlay.PositionOrigin = new Point(0.3, 0.9);
                driverDestinationIconOverlay.GeoCoordinate = new GeoCoordinate(destinationLatitude, destinationLongtitude);

                // Create a MapLayer to contain the MapOverlay.
                driverMapLayer = new MapLayer();
                driverMapLayer.Add(driverDestinationIconOverlay);

                // Add the MapLayer to the Map.
                map_DriverMap.Layers.Add(driverMapLayer);

                //Calculate Distance
                distanceMeter = Math.Round(GetTotalDistance(driverCoordinates), 0); //Round double in zero decimal places
            }
            else
            {
                MessageBox.Show("(Mã lỗi 305) " + ConstantVariable.errInvalidAddress);
            }
        }
        //------ END route Direction on Map ------//



        //private void getRouteTo(GeoCoordinate myPosition, GeoCoordinate destination)
        //{
        //    if (driverMapRoute != null)
        //    {
        //        map_DriverMap.RemoveRoute(driverMapRoute);
        //        driverMapRoute = null;
        //        driverQuery = null;
        //    }
        //    driverQuery = new RouteQuery()
        //    {
        //        TravelMode = TravelMode.Driving,
        //        Waypoints = new List<GeoCoordinate>()
        //    {
        //        myPosition, 
        //        destination
        //    },
        //        RouteOptimization = RouteOptimization.MinimizeTime
        //    };
        //    driverQuery.QueryCompleted += driverRouteQuery_QueryCompleted;
        //    driverQuery.QueryAsync();
        //}
        //void driverRouteQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        //{
        //    if (e.Error == null)
        //    {
        //        Route newRoute = e.Result;

        //        driverMapRoute = new MapRoute(newRoute);
        //        map_DriverMap.AddRoute(driverMapRoute);
        //        driverQuery.Dispose();
        //    }
        //}







        //------ BEGIN calculate Distance ------//
        private static double GetTotalDistance(IEnumerable<GeoCoordinate> coordinates)
        {
            double result = 0;

            if (coordinates.Count() > 1)
            {
                GeoCoordinate previous = coordinates.First();

                foreach (var current in coordinates)
                {
                    result += previous.GetDistanceTo(current);
                }
            }

            return result;
        }
        //------ END calculate Distance ------//





        //------ BEGIN show and Design UI 3 taxi near current position ------//
        private void ShowNearDrivers(double lat, double lng, string tName)
        {
            GeoCoordinate TaxiCoordinate = new GeoCoordinate(lat, lng);

            //Create taxi icon on map
            Image taxiIcon = new Image();
            taxiIcon.Source = new BitmapImage(new Uri("/Images/Taxis/img_CarIcon.png", UriKind.Relative));

            //Add a tapped event
            taxiIcon.Tap += taxiIcon_Tap;

            //Create Taxi Name 
            TextBlock taxiName = new TextBlock();
            taxiName.HorizontalAlignment = HorizontalAlignment.Center;
            taxiName.Text = tName;
            taxiName.FontSize = 12;
            taxiName.Foreground = new SolidColorBrush(Color.FromArgb(255, (byte)46, (byte)159, (byte)255)); //RBG color for #2e9fff

            //Create Stack Panel to group icon, taxi name, ...            
            Rectangle taxiNameBackground = new Rectangle();
            taxiNameBackground.Height = 18;
            taxiNameBackground.Width = taxiName.ToString().Length + 20;
            taxiNameBackground.RadiusX = 9;
            taxiNameBackground.RadiusY = 7;
            //taxiNameBackground.Stroke = new SolidColorBrush(Color.FromArgb(255, (byte)171, (byte)171, (byte)171)); //RBG color for #ababab
            taxiNameBackground.Fill = new SolidColorBrush(Color.FromArgb(255, (byte)213, (byte)235, (byte)255)); //RBG color for #d5ebff

            Grid taxiNameGrid = new Grid();
            taxiNameGrid.Margin = new Thickness(0, 4, 0, 4); //Margin Top and Bottom 4px
            taxiNameGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            taxiNameGrid.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            taxiNameGrid.Children.Add(taxiNameBackground);
            taxiNameGrid.Children.Add(taxiName);

            StackPanel taxiStackPanel = new StackPanel();
            //taxiStackPanel.Margin  = new Thickness(5, 0, 5, 0);
            taxiStackPanel.Children.Add(taxiIcon);
            taxiStackPanel.Children.Add(taxiNameGrid);

            // Create a MapOverlay to contain the circle.
            MapOverlay myTaxiOvelay = new MapOverlay();
            //myTaxiOvelay.Content = myCircle;
            myTaxiOvelay.Content = taxiStackPanel;
            myTaxiOvelay.PositionOrigin = new Point(0.5, 0.5);
            myTaxiOvelay.GeoCoordinate = TaxiCoordinate;

            //Add to Map's Layer
            driverMapLayer = new MapLayer();
            driverMapLayer.Add(myTaxiOvelay);

            map_DriverMap.Layers.Add(driverMapLayer);
        }

        //Tapped event
        private void taxiIcon_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }
        //------ END show and Design UI 3 taxi near current position ------//



        //------ BEGIN For open menu ------//
        private void btn_OpenMenu_Click(object sender, RoutedEventArgs e)
        {

            var left = Canvas.GetLeft(LayoutRoot);
            if (left > -100)
            {
                MoveViewWindow(-420);
            }
            else
            {
                MoveViewWindow(0);
            }
        }
        void MoveViewWindow(double left)
        {
            _viewMoved = true;
            ((Storyboard)canvas.Resources["moveAnimation"]).SkipToFill();
            ((DoubleAnimation)((Storyboard)canvas.Resources["moveAnimation"]).Children[0]).To = left;
            ((Storyboard)canvas.Resources["moveAnimation"]).Begin();
        }


        private void canvas_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            //if (e.DeltaManipulation.Translation.X != 0)
            //    Canvas.SetLeft(LayoutRoot, Math.Min(Math.Max(-840, Canvas.GetLeft(LayoutRoot) + e.DeltaManipulation.Translation.X), 0));
        }

        private void canvas_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            _viewMoved = false;
            initialPosition = Canvas.GetLeft(LayoutRoot);
        }

        private void canvas_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            var left = Canvas.GetLeft(LayoutRoot);
            if (_viewMoved)
                return;
            if (Math.Abs(initialPosition - left) < 100)
            {
                //bouncing back
                MoveViewWindow(initialPosition);
                return;
            }
            //change of state
            if (initialPosition - left > 0)
            {
                //slide to the left
                if (initialPosition > -420)
                    MoveViewWindow(-420);
                else
                    MoveViewWindow(-840);
            }
            else
            {
                //slide to the right
                if (initialPosition < -420)
                    MoveViewWindow(-420);
                else
                    MoveViewWindow(0);
            }
        }
        //------ END For open menu ------//





        //------ BEGIN Convert Lat & Lng from Address for Bing map Input ------//
        private void searchCoordinateFromAddress(string inputAddress)
        {
            //GoogleAPIGeocoding URL
            string URL = ConstantVariable.googleAPIGeocodingRequestsBaseURI + inputAddress + "&key=" + ConstantVariable.googleGeolocationAPIkey;

            //Query Autocomplete Responses to a JSON String
            WebClient proxy = new WebClient();
            proxy.DownloadStringCompleted +=
            new DownloadStringCompletedEventHandler(proxy_searchCoordinateFromAddress);
            proxy.DownloadStringAsync(new Uri(URL));
        }
        private void proxy_searchCoordinateFromAddress(object sender, DownloadStringCompletedEventArgs e)
        {
            //1. Convert Json String to an Object
            GoogleAPIGeocoding places = new GoogleAPIGeocoding();
            places = JsonConvert.DeserializeObject<GoogleAPIGeocoding>(e.Result);
            try
            {
                double lat = places.results[0].geometry.location.lat;
                double lng = places.results[0].geometry.location.lng;

                //route direction on map
                this.getMapRouteTo(lat, lng);
            }
            catch (Exception)
            {

                MessageBox.Show("(Mã lỗi 306) " + ConstantVariable.errInvalidAddress);
            }
        }

        //------ END Convert Lat & Lng from Address for Bing map Input ------//


        private void map_DriverMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "5fcbf5e6-e6d0-48d7-a69d-8699df1b5318";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "I5nG-B7z5bxyTGww1PApXA";
        }

        private void btn_ChangeStatus_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Update Status here
            // Change Button Color Here after change 
        }




        private void btn_Logout_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (tNetAppSetting.Contains("isLogin"))
            {
                tNetAppSetting.Remove("isLogin");
                tNetUserLoginData.Remove("UserId");
                tNetUserLoginData.Remove("PasswordMd5");
                NavigationService.Navigate(new Uri("/Pages/Login.xaml", UriKind.Relative));
            }
        }






        /// <summary>
        /// NOTIFICATION CHANNEL
        /// </summary>
        private void CreatePushChannel()
        {
            HttpNotificationChannel pushChannel;
            string channelName = "FtaxiDriverChannel";
            pushChannel = HttpNotificationChannel.Find(channelName);

            if (pushChannel == null)
            {
                pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                pushChannel.Open();

                // Bind this new channel for toast events.
                pushChannel.BindToShellToast();

            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());

                pushChannelURI = pushChannel.ChannelUri.ToString();
                UpdateNotificationURI(pushChannelURI);
                ///
                ///CODE UPDATE URI HERE///
                ///

            }
        }

        // Display the new URI for testing purposes.   Normally, the URI would be passed back to your web service at this point.
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {

            Dispatcher.BeginInvoke(() =>
            {
                System.Diagnostics.Debug.WriteLine(e.ChannelUri.ToString());
                pushChannelURI = e.ChannelUri.ToString();
                UpdateNotificationURI(pushChannelURI);
                ///
                ///CODE LOAD URI HERE///
                ///
            });
        }


        // Error handling logic for your particular application would be here.
        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }


        // Parse out the information that was part of the message.
        void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            Debug.WriteLine("Nhận được thông báo"); //DELETE AFTER FINISH


            StringBuilder message = new StringBuilder();
            //string relativeUri = string.Empty;

            //message.AppendFormat("Received Toast {0}:\n", DateTime.Now.ToShortTimeString());

            // Parse out the information that was part of the message.
            foreach (string key in e.Collection.Keys)
            {
                //message.AppendFormat("{0}: {1}\n", key, e.Collection[key]);

                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    //Lấy chuối thông báo từ Notification
                    notificationReceivedString = e.Collection[key];
                }
            }

            // Display a dialog of all the fields in the toast.
            //Dispatcher.BeginInvoke(() => MessageBox.Show(message.ToString()));          
            Dispatcher.BeginInvoke(() =>
            {
                //notificationReceivedString = e.Collection.Keys.;
                //MessageBox.Show(notificationReceivedString);
                if (notificationReceivedString != string.Empty)
                {

                    //Hàm này để lấy ra chuỗi Json trong một String gửi qua notification
                    int a = notificationReceivedString.IndexOf("json=");
                    int b = notificationReceivedString.IndexOf("}");
                    int c = notificationReceivedString.IndexOf("notiType=");
                    string tmpStirng = notificationReceivedString.Substring(a + 5, b - a - 4);
                    //Cái này để lấy kiểu 
                    notificationType = notificationReceivedString.Substring(c + 9, notificationReceivedString.Length - c - 9);
                    notificationReceivedString = tmpStirng;

                    //Sau đó chạy thông báo
                    ShowNotification();
                }
            });

        }


        /// <summary>
        /// hàm này để hiện thị thông báo lên màn hình
        /// Tham số sử dụng là chuỗi notificationReceivedString lấy ra từ kênh Notification
        /// </summary>
        private void ShowNotification()
        {
            //Nếu như có tồn tại chuỗi Json
            if (notificationReceivedString != string.Empty && notificationType != string.Empty)
            {
                switch (notificationType)
                {
                    case ConstantVariable.notiTypeNewTrip: //Nếu là "NT" thì sẽ chạy hàm Show New Trip Notification
                        ShowNotificationNewTrip();
                        break;
                    case ConstantVariable.notiTypeUpdateTrip: //Nếu là "UT" thì sẽ chạy hàm Show Update Trip Notification
                        ShowNotificationUpdateTrip();
                        break;
                    case ConstantVariable.notiTypePromotionTeip: //Nếu là "PT" thì sẽ chạy hàm Show Promotion Trip Notification
                        ShowNotificationPromotionTrip();
                        break;
                }
            }
            else
            {
                MessageBox.Show("(Mã lối 308) " + ConstantVariable.errServerError);
            }

        }



        /// <summary>
        /// Các trường hợp của thông báo
        /// </summary>
        private void ShowNotificationNewTrip()
        {
            var input = notificationReceivedString;
            try
            {
                var newTrip = JsonConvert.DeserializeObject<DriverNewtripNotification>(input); //Tạo đối tượng NewTrip từ Json Input
                //Nạp thông tin new trip vào grid Accept/Reject
                txt_RiderAddress.Text = newTrip.sAdd;
                txt_RiderMobile.Text = "0" + newTrip.mobile;
                txt_RiderName.Text = newTrip.rName;
                //Hiển thị thông tin new trip lên màn hình
                isGridAcceptRejectOn();

            }
            catch (Exception)
            {

                MessageBox.Show("(Mã lỗi 309) " + ConstantVariable.errHasErrInProcess);
            }
        }
        private void ShowNotificationUpdateTrip()
        {
        }
        private void ShowNotificationPromotionTrip()
        {
        }


        /// <summary>
        /// Nhận thông tin từ Notification
        /// Mỗi khi App không hoạt động (Ở màn hình Home, Lock, hay tắt màn hình) sẽ hiện thông báo
        /// khi ta nhấn vào thông báo, sẽ điều hướng tới trang /Pages/HomePage.xaml
        /// Và OnNavigatedTo là để lấy nội dung của thông báo
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            try
            {
                if (this.NavigationContext.QueryString["json"].ToString() != null)
                {
                    notificationReceivedString = this.NavigationContext.QueryString["json"].ToString(); //Gán chuỗi Json 
                    notificationType = this.NavigationContext.QueryString["notiType"].ToString(); //Gán kiểu noti
                    //Sau cùng là chạy hàm hiển thị notification
                    ShowNotification();
                }

            }
            catch (KeyNotFoundException)
            {
                //MessageBox.Show("(Mã lỗi 302) " + ConstantVariable.errServerError);
            }

        }


        /// <summary>
        /// Cái này là để cập nhật URI mỗi khi có thay đổi
        /// Cứ mỗi khi URI thay đổi, hệ thống sẽ cập nhật lên sv
        /// </summary>
        /// <param name="uri"></param>
        private async void UpdateNotificationURI(string uri)
        {
            var uid = userId;
            var mType = ConstantVariable.mTypeWIN;
            var role = ConstantVariable.dRole;
            var id = userData.content.driverInfo.did;
            var input = string.Format("{{\"uid\":\"{0}\",\"mid\":\"{1}\",\"mType\":\"{2}\",\"role\":\"{3}\",\"id\":\"{4}\"}}", uid, uri, mType, role, id);
            try
            {
                var output = await GetJsonFromPOSTMethod.GetJsonString(ConstantVariable.tNetDriverUpdateRegId, input);
            }
            catch (Exception)
            {
                //Lỗi máy chủ
                MessageBox.Show("(Mã lỗi 307) " + ConstantVariable.errServerError);
            }

        }


    }

}