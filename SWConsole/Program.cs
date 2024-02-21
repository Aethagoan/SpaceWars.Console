using PuppeteerSharp;
using SWConsole;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SpaceWarsServices;

class Program
{
    static async Task Main(string[] args)
    {
        //**************************************************************************************
        //***  |    |    |    |                                            |    |    |    |    |
        //***  |    |    |    |       Change your key mappings here        |    |    |    |    |
        //***  V    V    V    V                                            V    V    V    V    V
        //**************************************************************************************
        const ConsoleKey forwardKey = ConsoleKey.UpArrow;
        const ConsoleKey leftKey = ConsoleKey.LeftArrow;
        const ConsoleKey rightKey = ConsoleKey.RightArrow;
        const ConsoleKey downKey = ConsoleKey.DownArrow;
        const ConsoleKey fireKey = ConsoleKey.Spacebar;
        const ConsoleKey clearQueueKey = ConsoleKey.C;
        const ConsoleKey infoKey = ConsoleKey.I;
        const ConsoleKey shopKey = ConsoleKey.S;
        const ConsoleKey repairKey = ConsoleKey.R;
        const ConsoleKey readAndEmptyMessagesKey = ConsoleKey.M;

        // this should work much better.
        const int UPARROW = 0x26;
        const int DOWNARROW = 0x28;
        const int RIGHTARROW = 0x27;
        const int LEFTARROW = 0x25;
        const int SPACEBAR = 0x20; 
        const int PANICBUTTON = 0x43; // C
        const int REPAIRBUTTON = 0x52; // R
        const int INFOBUTTON = 0x49; // I



        // create pupeteer page
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();
        



        Uri baseAddress = getApiBaseAddress(args);
        using HttpClient httpClient = new HttpClient() { BaseAddress = baseAddress };
        bool exitGame = false;
        var currentHeading = 0;
        var token = "";
        var service = new ApiService(httpClient);
        List<PurchasableItem> Shop = new List<PurchasableItem>();
        JoinGameResponse joinGameResponse = null;

        Console.WriteLine("Please enter your name");
        var username = "Test1";



        try
        {
            joinGameResponse = await service.JoinGameAsync(username);
            token = joinGameResponse.Token;

            Shop = joinGameResponse.Shop.Select(item => new PurchasableItem(item.Cost, item.Name, item.Prerequisites)).ToList();

            Console.WriteLine($"Token:{joinGameResponse.Token}, Heading: {joinGameResponse.Heading}");
            Console.WriteLine($"Ship located at: {joinGameResponse.StartingLocation}, Game State is: {joinGameResponse.GameState}, Board Dimensions: {joinGameResponse.BoardWidth}, {joinGameResponse.BoardHeight}");

            OpenUrlInBrowser($"{baseAddress.AbsoluteUri}hud?token={token}");
            OpenUrlInBrowser($"{baseAddress.AbsoluteUri}/spectatorview");
        }
        catch (Exception ex)                                              
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        // pupeteer page go to the page
        await page.GoToAsync($"{baseAddress.AbsoluteUri}hud?token={token}");

        // how to get the current position from the page
        async Task<int[]> getCurrentPosition()
        {
            // is the p there?
            await page.WaitForSelectorAsync("p");
            var result = await page.EvaluateFunctionAsync<dynamic>(@"function getValues(){
                    ps = document.querySelectorAll(""p"")
                    newstring = ps[0].innerText.slice(16, ps[0].innerText.length - 1)

                    numbers = newstring.split("", "");

                    console.log(numbers)

                    return numbers
                }");

            /*Console.WriteLine("Puppeteer returned " +  result + " as the current position");*/

            int[] newarr = new int[2];
                
            for (int i = 0; i < 2; i++)
            {
                newarr[i] = result[i];
            }

            return newarr;
        }


        var gameActions = new GameActions(username, joinGameResponse, service);
        gameActions.Weapons.Add("Basic Cannon");
        gameActions.CurrentWeapon = "Basic Cannon";

        // thank you, reddit.
        [DllImport("user32.dll")]
        static extern int GetAsyncKeyState(int key);

        printStatus();

        int? lastAngle;

        while (!exitGame)
        {

            //panic button
            if (GetAsyncKeyState(PANICBUTTON) > 0 )
            {
                await gameActions.ClearQueueAsync();
            }

            //info extension
            if (GetAsyncKeyState(INFOBUTTON) > 0)
            {
               /* if ()*/
            }

            // movement
            // up is pressed
            if (GetAsyncKeyState(UPARROW) > 0)
            {
                // up, left
                if (GetAsyncKeyState(LEFTARROW) > 0)
                {
                    await gameActions.moveHeading(315);
                }
                // up right
                else if (GetAsyncKeyState (RIGHTARROW) > 0)
                {
                    await gameActions.moveHeading(45);
                }
                // just up
                else
                {
                    await gameActions.moveHeading(0);
                }
            }
            // down is pressed
            else if (GetAsyncKeyState(DOWNARROW) > 0)
            {
                // down, left
                if (GetAsyncKeyState(LEFTARROW) > 0)
                {
                    await gameActions.moveHeading(225);
                }
                // down, right
                else if (GetAsyncKeyState(RIGHTARROW) > 0)
                {
                    await gameActions.moveHeading(135);
                }
                // just down
                else
                {
                    await gameActions.moveHeading(180);
                }
            }
            // left
            else if (GetAsyncKeyState(LEFTARROW) > 0)
            {
                await gameActions.moveHeading(270);
            }
            // right
            else if (GetAsyncKeyState(RIGHTARROW) > 0)
            {
                await gameActions.moveHeading(90);
            }

            
            
            // are we shooting?
            if (GetAsyncKeyState(SPACEBAR) > 0)
            {
                // find the closest target

                // get my position
                var myposarr = getCurrentPosition();
                Vector2 currentpos = new Vector2((float)myposarr.Result[0], (float)myposarr.Result[1]);

                // get enemy positions
                GameStateResponse gameresponse = service.GetGameState().Result;

                /*Console.WriteLine("API Returned ");
                foreach (var local in gameresponse.PlayerLocations)
                {
                    Console.WriteLine(local.X + ", " + local.Y);
                }*/

                Vector2 enemypos = new();
                Vector2 closestEnemy;
                float? shortest;

                foreach (var location in gameresponse.PlayerLocations)
                {
                    if (location.X == currentpos.X && location.Y == currentpos.Y)
                    {
                        continue;
                    }

                    enemypos = new Vector2(location.X, location.Y);
                    var dist = Vector2.Distance(currentpos, enemypos);

                    if (shortest == null || dist < shortest)
                    {
                        shortest = dist;
                        closestEnemy = enemypos;
                    }

                }

                /*Console.WriteLine("current pos : " + currentpos.X + ", " +  currentpos.Y);
                Console.WriteLine("targeted enemy pos : " + closestEnemy.X + ", " +  closestEnemy.Y);
*/
                // we have closest enemy

                // calculate angle between them
                var radian = Math.Atan2((double)(closestEnemy.Y - currentpos.Y), (double)(closestEnemy.X - currentpos.X ));
                /*Console.WriteLine("radian: " + radian);*/

                var angle = ((int)( -1 * radian * (180 / Math.PI)) + 90) % 360;

                /*Console.WriteLine(angle);*/

                // change angle and fire!
                if (angle != lastAngle)
                {
                    lastAngle = angle;
                    
                    await gameActions.changeHeading(angle);
                }
                else
                {
                    await gameActions.FireWeaponAsync();
                }
                
                
            }

            //weapon switching




           /* switch ()
            {
                case var key when key == repairKey:
                    await gameActions.RepairShipAsync();
                    Console.WriteLine("Ship repair requested.");
                    break;
                case var key when key == infoKey:
                    foreach (var item in Shop)
                    {
                        Console.WriteLine($"upgrade: {item.Name}, cost: {item.Cost}");
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadKey();
                    }
                    break;
                case var key when key == shopKey:

                    Console.WriteLine("please enter what you'd like to purchase from the shop, (if you've changed your mind enter x)");
                    var response = Console.ReadLine();
                    if (response == "x")
                    {
                        continue;
                    }

                    if (Shop.Any(item => item.Name.Equals(response, StringComparison.OrdinalIgnoreCase)))
                    {
                        await gameActions.PurchaseItemAsync(response);
                        Console.WriteLine($"Purchase of {response} requested.");
                    }
                    else
                    {
                        Console.WriteLine("Invalid item. Please choose a valid item from the shop.");
                    }
                    break;
                case var key when key == readAndEmptyMessagesKey:
                    await gameActions.ReadAndEmptyMessagesAsync();
                    Console.WriteLine("Message queue read.");
                    break;
                case var key when key >= ConsoleKey.D0 && key <= ConsoleKey.D9:
                    gameActions.SelectWeapon(key);
                    Console.WriteLine($"Selected weapon {((char)key) - '1'} ({gameActions.CurrentWeapon}");
                    break;
                //**************************************************************************************
                //***  |    |    |    |                                            |    |    |    |    |
                //***  |    |    |    |       Add any other custom keys here       |    |    |    |    |
                //***  V    V    V    V                                            V    V    V    V    V
                //**************************************************************************************
                case ConsoleKey.N:
                    //example
                    break;
            }*/
        }

        void printStatus()
        {
            Console.Clear();
            Console.WriteLine($"Name: {username,-34}");
            Console.WriteLine($"Info-Mod-Key: I, Repair: R, Clear Queue: C, ARROW KEYS to move, SPACE to shoot.");

            for (int i = 0; i < Shop.Count; i++)
            {
                string? weapon = Shop[i].Name;
                if (weapon == gameActions.CurrentWeapon)
                {
                    weapon = $"**{Shop[i].Name}**";
                }
                Console.Write($"{i + 1}: {Shop[i].Name}   ");
            }
            Console.WriteLine();


            /*if (gameActions.GameMessages.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Last 10 messages:");
                Console.WriteLine(new string('-', Console.WindowWidth));
                foreach (var msg in gameActions.GameMessages.TakeLast(10))
                {
                    Console.WriteLine($"{msg.Type,-30} {msg.Message}");
                }
            }
            Console.WriteLine(new string('=', Console.WindowWidth));*/
        }


    }


    private static Uri getApiBaseAddress(string[] args)
    {
        Uri baseAddress;
        if (args.Length == 0)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Please enter the URL to access Space Wars");
                    baseAddress = new Uri(Console.ReadLine());
                    break;
                }
                catch { }
            }
        }
        else
        {
            baseAddress = new Uri(args[0]);
        }
        return baseAddress;
    }

    static void OpenUrlInBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening URL in browser: {ex.Message}");
        }
    }

    
}
