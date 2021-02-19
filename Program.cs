using System;
using System.Collections.Generic;
using SharpDX;
using WeScriptWrapper;
using WeScript.SDK.UI;
using WeScript.SDK.UI.Components;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace DeadByDaylight
{
    public class Program
    {
        public static float M_PI_F = (180.0f / Convert.ToSingle(System.Math.PI));
        public static IntPtr processHandle = IntPtr.Zero; //processHandle variable used by OpenProcess (once)
        public static bool gameProcessExists = false; //avoid drawing if the game process is dead, or not existent
        public static bool isWow64Process = true; //we all know the game is 32bit, but anyway...
        public static bool isGameOnTop = false; //we should avoid drawing while the game is not set on top
        public static bool isOverlayOnTop = false; //we might allow drawing visuals, while the user is working with the "menu"
        public static uint PROCESS_ALL_ACCESS = 0x1FFFFF; //hardcoded access right to OpenProcess (even EAC strips some of the access flags)
        public static Vector2 wndMargins = new Vector2(0, 0); //if the game window is smaller than your desktop resolution, you should avoid drawing outside of it
        public static Vector2 wndSize = new Vector2(0, 0); //get the size of the game window ... to know where to draw
        public static IntPtr GameBase = IntPtr.Zero;
        public static IntPtr GameSize = IntPtr.Zero;
        public static DateTime LastSpacePressedDT = DateTime.Now;
        public static IntPtr GWorldPtr = IntPtr.Zero;
        public static IntPtr GWorldPtr2 = IntPtr.Zero;
        public static IntPtr GNamesPtr = IntPtr.Zero;
        public static Vector2 GameCenterPos = new Vector2(0, 0);
        public static Vector2 GameCenterPos2 = new Vector2(0, 0);
        public static Vector3 FMinimalViewInfo_Location = new Vector3(0, 0, 0);
        public static Vector3 FMinimalViewInfo_Rotation = new Vector3(0, 0, 0);
        public static float FMinimalViewInfo_FOV = 0;
        public static Dictionary<UInt32, string> CachedID = new Dictionary<UInt32, string>();
        public static IntPtr FindWindow = IntPtr.Zero;

        public static bool wasAPressed = false;
        public static bool wasDPressed = false;
        public static uint survivorID = 0;
        public static uint killerID = 0;
        public static uint K22 = 0;
        public static uint K22Twin = 0;
        public static uint escapeID = 0;
        public static uint hatchID = 0;
        public static uint generatorID = 0;
        public static uint totemID = 0;
        public static uint HexID = 0;
        public static uint Pallet = 0;
        public static uint Locker = 0;
        public static uint Trap = 0;
        public static uint HagTrap = 0;
        public static uint DreamSnare = 0;
        public static uint Portal = 0;
        public static uint Chest = 0;
        public static uint Hook = 0;
        public static uint MedKit = 0;
        public static uint ToolBox = 0;
        public static uint Flashlight = 0;
        public static uint Key = 0;
        public static uint Jigsaw = 0;
        public static uint Wiggle = 0;
        public static Vector2 AimTarg2D = new Vector2(0, 0); //for aimbot
        public static Vector3 AimTarg3D = new Vector3(0, 0, 0);

        public enum EPalletState
        {
            Up = 0,
            Falling = 1,
            Fallen = 2,
            Destroyed = 3,
            Illusionary = 4,
            MAX = 5,

        };

        public static Menu RootMenu { get; private set; }
        public static Menu VisualsMenu { get; private set; }
        public static Menu MiscMenu { get; private set; }

        public static Menu AimbotMenuSurvivor { get; private set; }
        public static Menu AimbotMenuKiller { get; private set; }

        class Components
        {

            public static readonly MenuKeyBind MainAssemblyToggle = new MenuKeyBind("mainassemblytoggle", "Toggle the whole assembly effect by pressing key:", VirtualKeyCode.Delete, KeybindType.Toggle, true);
            public static class VisualsComponent
            {
                public static readonly MenuBool DrawTheVisuals = new MenuBool("drawthevisuals", "Enable all of the Visuals", true);
                public static readonly MenuColor SurvColor = new MenuColor("srvcolor", "Survivors Color", new SharpDX.Color(0, 255, 0, 60));
                public static readonly MenuBool DrawSurvivorBox = new MenuBool("srvbox", "Draw Survivors Box", true);
                public static readonly MenuBool DrawSurvivorLine = new MenuBool("srvlin", "Draw Survivors Line", true);
                public static readonly MenuColor KillerColor = new MenuColor("kilcolor", "Killers Color", new SharpDX.Color(255, 0, 0, 100));
                public static readonly MenuBool DrawKillerBox = new MenuBool("drawbox", "Draw Killer Box", true);
                public static readonly MenuBool DrawKillerLine = new MenuBool("drawlin", "Draw Killer Line", true);
                public static readonly MenuSlider DrawBoxThic = new MenuSlider("boxthickness", "Draw Box Thickness", 0, 0, 10);
                public static readonly MenuBool DrawBoxBorder = new MenuBool("drawboxborder", "Draw Border around Box and Text?", true);
                public static readonly MenuBool DrawEarnedBP = new MenuBool("drawboxborder", "Draw earned Bloodpoints", true);
                public static readonly MenuBool DrawMiscInfo = new MenuBool("drawmiscinfos", "Draw hatch/escape/totem positions", true);
                public static readonly MenuBool DrawOnlyHex = new MenuBool("onlyhexinfo", "Draw only HEX totems", true);
                //public static readonly MenuColor MiscColor = new MenuColor("misccolor", "Draw Text Color", new SharpDX.Color(255, 255, 255, 100));
                public static readonly MenuBool DrawGenerators = new MenuBool("drawgenerators", "Draw Generators positions", true);
                public static readonly MenuBool DrawLocker = new MenuBool("drawlockers", "Draw Lockers positions", true);
                public static readonly MenuBool DrawPallet = new MenuBool("drawpallets", "Draw Pallets positions", true);
                public static readonly MenuBool DrawChest = new MenuBool("drawchests", "Draw Chests positions", true);
                public static readonly MenuBool DrawTrap = new MenuBool("drawtraps", "Draw Traps positions", true);
                public static readonly MenuBool DrawHook = new MenuBool("drawHooks", "Draw Hooks positions", true);
                public static readonly MenuBool DrawItems = new MenuBool("drawKeys", "Draw Items positions", true);
                public static readonly MenuBool DrawWiggle = new MenuBool("drawWiggle", "Is Wiggling", true);
                //public static readonly MenuSlider OffsetGuesser = new MenuSlider("ofsgues", "Guess the offset", 10, 1, 250);
            }

            public static class AimbotComponentSurvivor
            {
                public static readonly MenuBool AimGlobalBool = new MenuBool("enableaim", "Enable Aimbot Features", true);
                public static readonly MenuKeyBind AimKey = new MenuKeyBind("aimkey", "Aimbot HotKey (HOLD)", VirtualKeyCode.LeftMouse, KeybindType.Hold, false);

                public static readonly MenuSlider AimSpeed = new MenuSlider("aimspeed", "Aimbot Speed %", 12, 1, 100);
                public static readonly MenuSlider Distance = new MenuSlider("Distance Killer", "Distance kill%", 12, 1, 100);
                public static readonly MenuSlider AimFov = new MenuSlider("aimfov", "Aimbot FOV", 100, 4, 1000);

                public static readonly MenuBool DrawFov = new MenuBool("DrawFOV", "Enable FOV Circle Features Survivor", true);
                public static readonly MenuKeyBind Wiggle = new MenuKeyBind("wigglekey", "Wiggle HotKey (HOLD)", VirtualKeyCode.T, KeybindType.Hold, false);
                public static readonly MenuKeyBind Struggle = new MenuKeyBind("strugglekey", "Struggle HotKey (HOLD)", VirtualKeyCode.Space, KeybindType.Hold, false);
                //public static readonly MenuBool AutoPallet = new MenuBool("AutoPallet", "Enable Auto Pallet Feature", true);
            }

            public static class AimbotComponentKiller
            {
                public static readonly MenuKeyBind AimKey = new MenuKeyBind("aimkey", "Aimbot HotKey (HOLD)", VirtualKeyCode.A, KeybindType.Hold, false);
                public static readonly MenuSlider AimSpeed = new MenuSlider("aimspeed", "Aimbot Speed %", 12, 1, 100);
                public static readonly MenuSlider AimFov = new MenuSlider("aimfov", "Aimbot FOV", 100, 4, 1000);
                public static readonly MenuBool AimGlobalBool = new MenuBool("enableaim", "Enable Aimbot Features", true);
                public static readonly MenuBool DrawFov = new MenuBool("DrawFOV", "Enable FOV Circle Features Killer", true);
            }


            public static class MiscComponent
            {
                public static readonly MenuBool AutoSkillCheck = new MenuBool("autoskillcheck", "Auto Skill Check (+Bonus)", true);
                public static readonly MenuBool SkinUnlock = new MenuBool("skins", "AllSkins", false);
                public static readonly MenuBool Attack = new MenuBool("attack", "attackspeed", false);
            }
        }

        public static void InitializeMenu()
        {
            VisualsMenu = new Menu("visualsmenu", "Visuals Menu")
            {
                Components.VisualsComponent.DrawTheVisuals,
                Components.VisualsComponent.SurvColor,
                Components.VisualsComponent.DrawSurvivorBox,
                Components.VisualsComponent.DrawSurvivorLine,
                Components.VisualsComponent.KillerColor,
                Components.VisualsComponent.DrawKillerBox,
                Components.VisualsComponent.DrawKillerLine,
                Components.VisualsComponent.DrawBoxThic.SetToolTip("Setting thickness to 0 will let the assembly auto-adjust itself depending on model distance"),
                //Components.VisualsComponent.DrawBoxBorder.SetToolTip("Drawing borders may take extra performance (FPS) on low-end computers"),
                Components.VisualsComponent.DrawMiscInfo,
                Components.VisualsComponent.DrawOnlyHex,
                //Components.VisualsComponent.MiscColor,
                Components.VisualsComponent.DrawGenerators,
                Components.VisualsComponent.DrawLocker,
                Components.VisualsComponent.DrawPallet,
                Components.VisualsComponent.DrawChest,
                Components.VisualsComponent.DrawTrap,
                Components.VisualsComponent.DrawHook,
                 Components.VisualsComponent.DrawItems,
                 Components.VisualsComponent.DrawWiggle,
                 
                //Components.VisualsComponent.OffsetGuesser,
            };

            AimbotMenuSurvivor = new Menu("aimbotmenu", "Aimbot Killer Menu")
            {
                Components.AimbotComponentKiller.AimGlobalBool,
                Components.AimbotComponentKiller.AimKey,
                Components.AimbotComponentKiller.AimSpeed,
                Components.AimbotComponentKiller.AimFov,
                Components.AimbotComponentKiller.DrawFov,

            };

            AimbotMenuKiller = new Menu("aimbotmenuSurvivor", "Survivor Menu")
            {

                Components.AimbotComponentSurvivor.AimGlobalBool,
                Components.AimbotComponentSurvivor.AimKey,
                Components.AimbotComponentSurvivor.AimSpeed,
                Components.AimbotComponentSurvivor.AimFov,
                Components.AimbotComponentSurvivor.DrawFov,
                Components.AimbotComponentSurvivor.Wiggle,
                Components.AimbotComponentSurvivor.Struggle,
                //Components.AimbotComponentSurvivor.AutoPallet,
            };

            MiscMenu = new Menu("miscmenu", "Misc Menu")
            {
                Components.MiscComponent.AutoSkillCheck,
                Components.MiscComponent.SkinUnlock,
                Components.VisualsComponent.DrawEarnedBP,


            };


            RootMenu = new Menu("dbdexample", "Hellow, Inverse here, have fun", true)
            {
                Components.MainAssemblyToggle.SetToolTip("The magical boolean which completely disables/enables the assembly!"),
                VisualsMenu,
                MiscMenu,
                AimbotMenuSurvivor,
                AimbotMenuKiller,
            };
            RootMenu.Attach();
        }



        private static double GetDistance2D(Vector2 pos1, Vector2 pos2)
        {
            Vector2 vector = new Vector2(pos1.X - pos2.X, pos1.Y - pos2.Y);
            return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        }

        private static Vector3 ReadBonePos(IntPtr playerPtr, int boneIDX)
        {
            //#define OFFSET_MeshToWorld 0x1E0
            // OFFSET_MeshArray 0x4A0
            Vector3 targetVec = new Vector3(0, 0, 0);
            var ActorPawn = Memory.ReadPointer(processHandle, playerPtr + 0x128, isWow64Process);
            var mesh = Memory.ReadPointer(processHandle, ActorPawn + 0x0290, isWow64Process);
            //Console.WriteLine(ActorPawn);
            var BoneMatrixPtr = Memory.ReadPointer(processHandle, (mesh + 0x4A0), isWow64Process); //m_dwBoneMatrix
            if (BoneMatrixPtr != IntPtr.Zero)
            {
                targetVec.X = Memory.ReadFloat(processHandle, (BoneMatrixPtr + 0x30 * boneIDX + 0x0C));
                targetVec.Y = Memory.ReadFloat(processHandle, (BoneMatrixPtr + 0x30 * boneIDX + 0x1C));
                targetVec.Z = Memory.ReadFloat(processHandle, (BoneMatrixPtr + 0x30 * boneIDX + 0x2C));
            }
            return targetVec;
        }

        public static Vector3 ClampAngle(Vector3 angle)
        {
            while (angle.Y > 180) angle.Y -= 360;
            while (angle.Y < -180) angle.Y += 360;

            if (angle.X > 89.0f) angle.X = 89.0f;
            if (angle.X < -89.0f) angle.X = -89.0f;

            angle.Z = 0f;

            return angle;
        }

        public static void AutoSkillCheck(IntPtr USkillCheck)
        {
            if (USkillCheck != IntPtr.Zero)
            {

                var isDisplayed = Memory.ZwReadBool(processHandle, (IntPtr)USkillCheck.ToInt64() + 0x02A8);
                if (isDisplayed)
                {
                    var currentProgress = Memory.ZwReadFloat(processHandle,
                        (IntPtr)USkillCheck.ToInt64() + 0x0248); //0x02A0
                    var startSuccessZone = Memory.ZwReadFloat(processHandle,
                        (IntPtr)USkillCheck.ToInt64() + 0x0290);

                    if (currentProgress > startSuccessZone)
                    {
                        Input.KeyPress(VirtualKeyCode.Space);
                    }
                }
            }
        }

        public static void SkinUnlocker()
        {
            var Address = Memory.ZwReadInt64(processHandle, GameBase + 0x7E67508);
            Memory.ZwWriteBool(processHandle, (IntPtr)Address, true);
        }

        public static void Attacks() // DiableHUD
        {
            ////var Address = Memory.ZwReadInt64(processHandle, GameBase + 0x7EA1450);
            //var Address = Memory.ZwReadInt64(processHandle, GameBase + 0x7EE8230);//7ED9768
            //Memory.ZwWriteBool(processHandle, (IntPtr)Address, true);
        }


        static System.Threading.Timer TimerWiggleStruggle = new System.Threading.Timer(WiggleStruggleTimer, 5, 0, 40);
        static int wigglecount = 0;
        static int strugglecount = 0;
        private static void WiggleStruggleTimer(Object state)
        {
            if ((!isGameOnTop) && (!isOverlayOnTop)) return;

            if (Components.AimbotComponentSurvivor.Struggle.Enabled)
            {
                if (strugglecount == 0)
                { Input.keybd_eventWS(VirtualKeyCode.Space, 0, KEYEVENTF.KEYDOWN, IntPtr.Zero); strugglecount++; goto done; }
                if (strugglecount == 1)
                { Input.keybd_eventWS(VirtualKeyCode.Space, 0, KEYEVENTF.KEYUP, IntPtr.Zero); ; strugglecount = 0; }
            }
        done:
            if (Components.AimbotComponentSurvivor.Wiggle.Enabled)
            {
                if (wigglecount == 0)
                { Input.keybd_eventWS(VirtualKeyCode.A, 0, KEYEVENTF.KEYDOWN, IntPtr.Zero); wigglecount++; return; }
                if (wigglecount == 1)
                { Input.keybd_eventWS(VirtualKeyCode.A, 0, KEYEVENTF.KEYUP, IntPtr.Zero); wigglecount++; return; }
                if (wigglecount == 2)
                { Input.keybd_eventWS(VirtualKeyCode.D, 0, KEYEVENTF.KEYDOWN, IntPtr.Zero); wigglecount++; return; }
                if (wigglecount == 3)
                { Input.keybd_eventWS(VirtualKeyCode.D, 0, KEYEVENTF.KEYUP, IntPtr.Zero); wigglecount = 0; return; }
            } //add thread.sleep
        }

        public static Vector3 NormalizeAngle(Vector3 angle)
        {
            while (angle.X < -180.0f) angle.X += 360.0f;
            while (angle.X > 180.0f) angle.X -= 360.0f;

            while (angle.Y < -180.0f) angle.Y += 360.0f;
            while (angle.Y > 180.0f) angle.Y -= 360.0f;

            while (angle.Z < -180.0f) angle.Z += 360.0f;
            while (angle.Z > 180.0f) angle.Z -= 360.0f;

            return angle;
        }

        public static Vector3 CalcAngle(Vector3 playerPosition, Vector3 enemyPosition, Vector3 aimPunch, Vector3 vecView, float yawRecoilReductionFactory, float pitchRecoilReductionFactor)
        {
            Vector3 delta = new Vector3(playerPosition.X - enemyPosition.X, playerPosition.Y - enemyPosition.Y, (playerPosition.Z + vecView.Z) - enemyPosition.Z);

            Vector3 tmp = Vector3.Zero;
            tmp.X = Convert.ToSingle(System.Math.Atan(delta.Z / System.Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y))) * 57.295779513082f - aimPunch.X * yawRecoilReductionFactory;
            tmp.Y = Convert.ToSingle(System.Math.Atan(delta.Y / delta.X)) * M_PI_F - aimPunch.Y * pitchRecoilReductionFactor;
            tmp.Z = 0;

            if (delta.X >= 0.0) tmp.Y += 180f;

            tmp = NormalizeAngle(tmp);
            tmp = ClampAngle(tmp);

            return tmp;
        }

        public static void SigScan()
        {
            //GWorldPtr = Memory.ZwFindSignature(processHandle, GameBase, GameSize, "48 89 05 ? ? ? ? 0F 28 D7", 0x3); //4.2
            GWorldPtr = Memory.ZwReadPointer(processHandle, GameBase + 0x8467600, isWow64Process);
            //Console.WriteLine($"GWorldPtr: {GWorldPtr.ToString("X")}");

            GNamesPtr = GameBase + 0x8298D00;

            //GNamesPtr = Memory.ZwFindSignature(processHandle, GameBase, GameSize, "48 8D 1D ? ? ? ? EB 16 48 8D 0D", 0x3);// 4.2
            //Console.WriteLine($"GNamesPtr: {GNamesPtr.ToString("X")}");
        }


        public static string GetNameFromID(uint ID) //really bad implementation - probably needs fixing, plus it's better to use it as a dumper once at startup and cache ids
        {
            if (processHandle != IntPtr.Zero)
            {
                if (GameBase != IntPtr.Zero)
                {

                    uint BlockIndex = ID >> 16;
                    var Address = Memory.ZwReadPointer(processHandle, (IntPtr)(GNamesPtr.ToInt64() + 0x10 + BlockIndex * 8), isWow64Process);
                    if (Address != IntPtr.Zero)
                    {
                        var Offset = ID & 65535;
                        var NameAddress = (IntPtr)(Address.ToInt64() + Offset * 4);
                        var tempID = Memory.ZwReadDWORD(processHandle, NameAddress);
                        if (tempID == ID)
                        {
                            var charLen = Memory.ZwReadWORD(processHandle, (IntPtr)(NameAddress.ToInt64() + 4));
                            if (charLen > 0)
                            {
                                var name = Memory.ZwReadString(processHandle, (IntPtr)(NameAddress.ToInt64() + 6), false, charLen);
                                if (name.Length > 0) return name;
                            }
                        }
                    }
                }
            }
            return "NULL";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("HELLOW");
            InitializeMenu();
            if (!Memory.InitDriver(DriverName.nsiproxy)) { Console.WriteLine("  [ERROR] EAC Bypass not initialized. \n  Are you not a VIP member?"); }
            Renderer.OnRenderer += OnRenderer;
            Memory.OnTick += OnTick;
        }
        public static double dims = 0.01905f;
        private static double GetDistance3D(Vector3 myPos, Vector3 enemyPos)
        {
            Vector3 vector = new Vector3(myPos.X - enemyPos.X, myPos.Y - enemyPos.Y, myPos.Z - enemyPos.Z);
            return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z) * dims;
        }

        private static void OnTick(int counter, EventArgs args)
        {

            if (FindWindow == IntPtr.Zero) FindWindow = Memory.FindWindowName("DeadByDaylight  ");
            if (FindWindow != IntPtr.Zero)
            {
                var calcPid = Memory.GetPIDFromHWND(FindWindow);
                gameProcessExists = true;
                if (processHandle == IntPtr.Zero) processHandle = Memory.ZwOpenProcess(PROCESS_ALL_ACCESS, calcPid);

                if (processHandle != IntPtr.Zero)
                {
                    if (GameBase == IntPtr.Zero) GameBase = Memory.ZwGetModule(processHandle, null, isWow64Process);
                    if (GameBase != IntPtr.Zero)
                    {
                        //bool EscapeDoorActivated = Memory.ZwReadBool(processHandle, GameBase + 0x2b1f660);
                        //UInt32 ActivatedGeneratorCount = Memory.ZwReadUInt32(processHandle, GameBase + 0x2b20080);
                        //Console.WriteLine(EscapeDoorActivated + "" + ActivatedGeneratorCount);
                        //Console.WriteLine(GameBase.ToString("X"));
                        GameSize = Memory.ZwGetModuleSize(processHandle, null, isWow64Process);
                        if (GWorldPtr == IntPtr.Zero && GNamesPtr == IntPtr.Zero) SigScan();
                        if (GWorldPtr != IntPtr.Zero && GNamesPtr != IntPtr.Zero)
                        {

                            wndMargins = Renderer.GetWindowMargins(FindWindow);
                            wndSize = Renderer.GetWindowSize(FindWindow);
                            isGameOnTop = Renderer.IsGameOnTop(FindWindow);
                            GameCenterPos = new Vector2(wndSize.X / 2 + wndMargins.X, wndSize.Y / 2 + wndMargins.Y);
                            GameCenterPos2 = new Vector2(wndSize.X / 2 + wndMargins.X, wndSize.Y / 2 + wndMargins.Y + 750.0f);//even if the game is windowed, calculate perfectly it's "center" for aim or crosshair
                            isOverlayOnTop = Overlay.IsOnTop();
                        }
                    }
                }

            }
        }

        private static void OnRenderer(int fps, EventArgs args)
        {
            if (!gameProcessExists) return; //process is dead, don't bother drawing
            if ((!isGameOnTop) && (!isOverlayOnTop)) return; //if game and overlay are not on top, don't draw
            if (!Components.MainAssemblyToggle.Enabled) return; //main menu boolean to toggle the cheat on or off

            double fClosestPos = 999999;
            AimTarg2D = new Vector2(0, 0);
            AimTarg3D = new Vector3(0, 0, 0);

            var ControllerRotation = IntPtr.Zero;
            float Score = 0;
            var USkillCheck = IntPtr.Zero;
            uint Name = 0;
            uint _currentHealthStateCount = 0;
            Functions.Ppc(out ControllerRotation, out Score, out _currentHealthStateCount, out Name, out USkillCheck);


            if (Components.MiscComponent.AutoSkillCheck.Enabled)
            {
                AutoSkillCheck(USkillCheck);
            }

            if (Components.MiscComponent.SkinUnlock.Enabled)
            {
                SkinUnlocker();
            }

            var gameState = Memory.ZwReadPointer(processHandle, (IntPtr)(GWorldPtr.ToInt64() + Offsets.UE.UWorld.gameState), true);
            var _searchables = Memory.ZwReadPointer(processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._searchables), true);
            var Ucollectable = Memory.ZwReadPointer(processHandle, (IntPtr)(_searchables.ToInt64() + 0x02F8), true);
            var itemId = Memory.ZwReadUInt32(processHandle, (IntPtr)(Ucollectable.ToInt64() + 0x02D0));
            uint _itemIdToSpawn = Memory.ZwReadUInt32(processHandle, (IntPtr)(_searchables.ToInt64() + 0x02E8));

            var n1 = GetNameFromID(itemId);
            var n2 = GetNameFromID(_itemIdToSpawn);

            //var FOV = Memory.ZwReadFloat(processHandle, (IntPtr)(Ucollectable.ToInt64() + 0x02D0));


            var ULevel = Memory.ZwReadPointer(processHandle, GWorldPtr + 0x38, isWow64Process);
            if (ULevel != IntPtr.Zero)
            {

                var AActors = Memory.ZwReadPointer(processHandle, (IntPtr)ULevel.ToInt64() + 0xA0, isWow64Process);
                var ActorCnt = Memory.ZwReadUInt32(processHandle, (IntPtr)ULevel.ToInt64() + 0xA8);
                if ((AActors != IntPtr.Zero) && (ActorCnt > 0))
                {
                    for (uint i = 0; i <= ActorCnt; i++)
                    {
                        var AActor = Memory.ZwReadPointer(processHandle, (IntPtr)(AActors.ToInt64() + i * 8),
                            isWow64Process);
                        if (AActor != IntPtr.Zero)
                        {
                            //Console.WriteLine(AActor.ToString("X"));
                            //var Health = Memory.ZwReadUInt32(processHandle, (IntPtr)AActor.ToInt64() + Offsets.UE.AActor.Health);
                            var AActorID = Memory.ZwReadUInt32(processHandle, (IntPtr)AActor.ToInt64() + 0x18);
                            if (!CachedID.ContainsKey(AActorID))
                            {
                                var retname = GetNameFromID(AActorID);
                                CachedID.Add(AActorID, retname);
                            }
                            var USceneComponent = Memory.ZwReadPointer(processHandle, (IntPtr)AActor.ToInt64() + 0x140, isWow64Process);
                            if (USceneComponent != IntPtr.Zero)
                            {
                                var tempVec2 = Memory.ZwReadVector3(processHandle,
                                    (IntPtr)USceneComponent.ToInt64() + 0x118);

                                //var UDBDOutlineComponent = Memory.ZwReadPointer(processHandle, (IntPtr)AActor.ToInt64() + 0x0240, true);
                                //var OutlineName = Memory.ZwReadUInt32(processHandle, (IntPtr)UDBDOutlineComponent.ToInt64() + 0x0298);
                                //var AlwaysVisible = Memory.ZwReadBool(processHandle, (IntPtr)UDBDOutlineComponent.ToInt64() + 0x02C9);

                                //Console.WriteLine(GetNameFromID(OutlineName));

                                var retname = CachedID[AActorID];

                                if ((AActorID > 0)) //&& (AActorID < 700000)
                                {
                                    if ((survivorID == 0) || (killerID == 0) || (escapeID == 0) ||
                                        (hatchID == 0) || (generatorID == 0) || (totemID == 0) || (HexID == 0))
                                    {
                                        if (retname.Contains("BP_CamperFemale")) survivorID = AActorID;
                                        if (retname.Contains("BP_CamperMale")) survivorID = AActorID;
                                        if (retname.Contains("BP_Slasher_Character_")) killerID = AActorID;
                                        if (retname.Contains("_20_C")) killerID = AActorID;

                                        if (retname.Contains("Twin")) killerID = AActorID;

                                        if (retname.Contains("BearTrap_001")) Trap = AActorID;
                                        if (retname.Contains("PhantomTrap")) HagTrap = AActorID;
                                        if (retname.Contains("DreamSnare_C")) DreamSnare = AActorID;
                                        if (retname.Contains("GroundPortal")) Portal = AActorID;
                                        if (retname.Contains("ReverseBearTrapRemover")) Jigsaw = AActorID;
                                        if (retname.Contains("Wiggle")) Wiggle = AActorID;
                                        if (retname.ToLower().Contains("kit")) MedKit = AActorID;
                                        if (retname.ToLower().Contains("toolbox")) ToolBox = AActorID;
                                        if (retname.ToLower().Contains("flashlight")) Flashlight = AActorID;
                                        if (retname.Contains("Key")) Key = AActorID;

                                        //if (retname.Contains("Window_Vault")) Window = AActorID;
                                        if (retname.Contains("SmallMeatLocker")) Hook = AActorID;
                                        if (retname.Contains("ClosetStandard")) Locker = AActorID;
                                        if (retname.Contains("Bookshelf")) Pallet = AActorID;
                                        if (retname.ToLower().Contains("totem")) totemID = AActorID;
                                        if (retname.Contains("EntityWall")) escapeID = AActorID;
                                        if (retname.Contains("BP_Hatch")) hatchID = AActorID;
                                        if (retname.Contains("Chest")) Chest = AActorID;
                                        if (retname.StartsWith("Generator")) generatorID = AActorID;
                                    }
                                }
                                var tempVec = Memory.ZwReadVector3(processHandle, (IntPtr)USceneComponent.ToInt64() + 0x118);
                                int dist = (int)(GetDistance3D(FMinimalViewInfo_Location, tempVec));


                                if (dist < 300)
                                {

                                    /*   if (Components.AimbotComponentSurvivor.Wiggle.Enabled)
                                   {
                                       Vector2 vScreen_d3d111 = new Vector2(0, 0);
                                       if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d111, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                       {
                                           var gnm = GetNameFromID(AActorID);
                                           if (gnm != "NULL")
                                           {
                                               Renderer.DrawText($"ID: {AActorID.ToString()} {gnm}", vScreen_d3d111, Color.White, 12, TextAlignment.centered, false);
                                           }
                                           else
                                           {
                                               Renderer.DrawText($"ID: {AActorID.ToString()} {gnm}", vScreen_d3d111, Color.Red, 12, TextAlignment.centered, false);
                                           }
                                       }
                                   } */


                                    Vector2 vScreen_d3d11xx = new Vector2(0, 0);
                                    if (AActorID == survivorID)
                                    {
                                        if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11xx, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                        {
                                            //Console.WriteLine(Health);
                                            //2884479152 injured

                                            if (Components.VisualsComponent.DrawEarnedBP.Enabled)
                                            {
                                                if (dist < 5)
                                                    Renderer.DrawText("BP = " + Score, vScreen_d3d11xx, Color.HotPink, 20, TextAlignment.centered, true);
                                            }

                                            //if (AActorID == killerID && dist < 50)
                                            //    Renderer.DrawText("RUN", vScreen_d3d11xx, Color.Red, 50);
                                        }
                                    }



                                    if (Components.VisualsComponent.DrawTheVisuals.Enabled)
                                    {

                                        if (AActorID == survivorID)
                                        {
                                            //Console.WriteLine(retname);
                                            //Renderer.DrawText("" + retname, vScreen_d3d11xx, Color.HotPink, 20, TextAlignment.centered, true);
                                            // for killer//
                                            Vector2 vScreen_h3adSurvivor = new Vector2(0, 0);
                                            Vector2 vScreen_f33tSurvivor = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 70.0f), out vScreen_h3adSurvivor, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 130.0f), out vScreen_f33tSurvivor, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize);

                                                if (Components.AimbotComponentKiller.DrawFov.Enabled)
                                                {
                                                    Renderer.DrawCircle(GameCenterPos, Components.AimbotComponentKiller.AimFov.Value, Color.White);
                                                }


                                                string survivorname = "SURVIVOR";
                                                if (retname.Contains("BP_CamperFemale01_Character")) survivorname = "Meg";
                                                if (retname.Contains("BP_CamperFemale02_Character")) survivorname = "Claudette";
                                                if (retname.Contains("BP_CamperFemale03_Character")) survivorname = "Nea";
                                                if (retname.Contains("BP_CamperFemale04_Character")) survivorname = "Laurie";
                                                if (retname.Contains("BP_CamperFemale05_Character")) survivorname = "Feng";
                                                if (retname.Contains("BP_CamperFemale06_Character")) survivorname = "Kate";
                                                if (retname.Contains("BP_CamperFemale07_Character")) survivorname = "Jane";
                                                if (retname.Contains("BP_CamperFemale08_Character")) survivorname = "Nancy";
                                                if (retname.Contains("BP_CamperFemale09_Character")) survivorname = "Yui";
                                                if (retname.Contains("BP_CamperFemale10_Character")) survivorname = "Zarina";
                                                if (retname.Contains("BP_CamperFemale11_Character")) survivorname = "Cheryl";
                                                if (retname.Contains("BP_CamperFemale12_Character")) survivorname = "Elodie";
                                                if (retname.Contains("BP_CamperMale01")) survivorname = "Dwight";
                                                if (retname.Contains("BP_CamperMale02_Character")) survivorname = "Jake";
                                                if (retname.Contains("BP_CamperMale03_Character")) survivorname = "Ace";
                                                if (retname.Contains("BP_CamperMale04_Character")) survivorname = "Bill";
                                                if (retname.Contains("BP_CamperMale05_Character")) survivorname = "David";
                                                if (retname.Contains("BP_CamperMale06_Character")) survivorname = "Quentin";
                                                if (retname.Contains("BP_CamperMale07_Character")) survivorname = "Tapp";
                                                if (retname.Contains("BP_CamperMale08_Character")) survivorname = "Adam";
                                                if (retname.Contains("BP_CamperMale09_Character")) survivorname = "Jeff";
                                                if (retname.Contains("BP_CamperMale10_Character")) survivorname = "Ashley";
                                                if (retname.Contains("BP_CamperMale11_Character")) survivorname = "Steve";
                                                if (retname.Contains("BP_CamperMale12_Character")) survivorname = "Felix";

                                                long PlayerStateAddress = Memory.ZwReadInt64(processHandle, (IntPtr)(AActor.ToInt64() + 0x250));
                                                long PlayerNameAddress = Memory.ZwReadInt64(processHandle, (IntPtr)PlayerStateAddress + 0x318);
                                                int PlayerNameLengthPlus1 = (int)Memory.ZwReadInt64(processHandle, (IntPtr)PlayerStateAddress + 0x318 + 0x8);
                                                string playername = Memory.ZwReadString(processHandle, (IntPtr)PlayerNameAddress, true, PlayerNameLengthPlus1);

                                                if (Components.VisualsComponent.DrawSurvivorLine.Enabled && dist > 5)
                                                {

                                                    Renderer.DrawLine(GameCenterPos2, vScreen_h3adSurvivor, Components.VisualsComponent.SurvColor.Color, Components.VisualsComponent.DrawBoxThic.Value);
                                                    Renderer.DrawText(survivorname + " [" + dist + "]", vScreen_f33tSurvivor.X, vScreen_f33tSurvivor.Y + 5, Components.VisualsComponent.SurvColor.Color, 12, TextAlignment.centered, false);
                                                    Renderer.DrawText(playername, vScreen_f33tSurvivor.X, vScreen_f33tSurvivor.Y + 16, Components.VisualsComponent.SurvColor.Color, 12, TextAlignment.centered, false);


                                                }
                                                if (Components.VisualsComponent.DrawSurvivorBox.Enabled && dist > 5)
                                                {
                                                    Renderer.DrawFPSBox(vScreen_h3adSurvivor, vScreen_f33tSurvivor, Components.VisualsComponent.SurvColor.Color, BoxStance.standing, Components.VisualsComponent.DrawBoxThic.Value, Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                    Renderer.DrawText(survivorname + " [" + dist + "]", vScreen_f33tSurvivor.X, vScreen_f33tSurvivor.Y + 5, Components.VisualsComponent.SurvColor.Color, 12, TextAlignment.centered, false);
                                                    Renderer.DrawText(playername, vScreen_f33tSurvivor.X, vScreen_f33tSurvivor.Y + 16, Components.VisualsComponent.SurvColor.Color, 12, TextAlignment.centered, false);

                                                }





                                                //if (PlayerNameLengthPlus1 > 0)
                                                //playername = ReadStringUnicode(PlayerNameAddress, PlayerNameLengthPlus1);

                                                //(IntPtr)AActor.ToInt64()

                                                /*long PlayerStateAddress = Marshal.ReadInt64((IntPtr)AActor.ToInt64() + 0x250);
                                                int len = (int)Marshal.ReadInt64((IntPtr)PlayerStateAddress + 0x318 + 0x8);
                                                if (len > 0)
                                                {
                                                    int pointer = (int)Marshal.ReadInt64((IntPtr)PlayerStateAddress + 0x318);
                                                    if (pointer != 0)
                                                    {
                                                        byte[] chars = { 0 };
                                                        int hey = MemoryStream.Read(chars, pointer, len * 2);
                                                        if (chars.Length > 0)
                                                        {
                                                            foreach (var letter in chars)
                                                            {
                                                                if (letter > 0)
                                                                {
                                                                    playername += Convert.ToChar(letter);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }*/



                                                var AimDist2D = GetDistance2D(vScreen_h3adSurvivor, GameCenterPos);
                                                if (Components.AimbotComponentKiller.AimFov.Value < AimDist2D) continue;

                                                if (AimDist2D < fClosestPos)
                                                {
                                                    fClosestPos = AimDist2D;
                                                    AimTarg2D = vScreen_h3adSurvivor;


                                                    if (Components.AimbotComponentKiller.AimKey.Enabled && Components.AimbotComponentKiller.AimGlobalBool.Enabled && dist <= 50)
                                                    {

                                                        double DistX = 0;
                                                        double DistY = 0;
                                                        DistX = (AimTarg2D.X) - GameCenterPos.X;
                                                        DistY = (AimTarg2D.Y) - GameCenterPos.Y;

                                                        double slowDistX = DistX / (1.0f + (Math.Abs(DistX) / (1.0f + Components.AimbotComponentKiller.AimSpeed.Value)));
                                                        double slowDistY = DistY / (1.0f + (Math.Abs(DistY) / (1.0f + Components.AimbotComponentKiller.AimSpeed.Value)));
                                                        Input.mouse_eventWS(MouseEventFlags.MOVE, (int)slowDistX, (int)slowDistY, MouseEventDataXButtons.NONE, IntPtr.Zero);

                                                        //Vector3 Aimassist = new Vector3()

                                                    }

                                                }
                                            }
                                        }


                                        //for survivor//
                                        if (AActorID == killerID)
                                        {
                                            Vector2 vScreen_h3adKiller = new Vector2(0, 0);
                                            Vector2 vScreen_f33tKiller = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 80.0f), out vScreen_h3adKiller, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 150.0f), out vScreen_f33tKiller, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize);

                                                string killername = "KILLER";
                                                if (retname.Contains("BP_Slasher_Character_01_C")) killername = "Trapper";
                                                if (retname.Contains("BP_Slasher_Character_02_C")) killername = "Wraith";
                                                if (retname.Contains("BP_Slasher_Character_03_C")) killername = "Hillbilly";
                                                if (retname.Contains("BP_Slasher_Character_04_C")) killername = "Nurse";
                                                if (retname.Contains("BP_Slasher_Character_05_C")) killername = "Hag";
                                                if (retname.Contains("BP_Slasher_Character_06_C")) killername = "Myers";
                                                if (retname.Contains("BP_Slasher_Character_07_C")) killername = "Doctor";
                                                if (retname.Contains("BP_Slasher_Character_08_C")) killername = "Huntress";
                                                if (retname.Contains("BP_Slasher_Character_09_C")) killername = "Leatherface";
                                                if (retname.Contains("BP_Slasher_Character_10_C")) killername = "Freddy";
                                                if (retname.Contains("BP_Slasher_Character_11_C")) killername = "Pig";
                                                if (retname.Contains("BP_Slasher_Character_12_C")) killername = "Clown";
                                                if (retname.Contains("BP_Slasher_Character_13_C")) killername = "Spirit";
                                                if (retname.Contains("BP_Slasher_Character_14_C")) killername = "Legion";
                                                if (retname.Contains("BP_Slasher_Character_15_C")) killername = "Plague";
                                                if (retname.Contains("BP_Slasher_Character_16_C")) killername = "Ghost Face";
                                                if (retname.Contains("BP_Slasher_Character_17_C")) killername = "Demogorgon";
                                                if (retname.Contains("BP_Slasher_Character_18_C")) killername = "Oni";
                                                if (retname.Contains("BP_Slasher_Character_19_C")) killername = "Deathslinger";
                                                if (retname.Contains("_20_C")) killername = "Pyramid Head";
                                                if (retname.Contains("BP_Slasher_Character_21_C")) killername = "Blight";
                                                if (retname.Contains("BP_Slasher_Character_22_C")) killername = "Twins";
                                                if (retname.Contains("Twin")) killername = "Victor";

                                                string playername = " ";
                                                long PlayerStateAddress = Memory.ZwReadInt64(processHandle, (IntPtr)(AActor.ToInt64() + 0x250));
                                                long PlayerNameAddress = Memory.ZwReadInt64(processHandle, (IntPtr)PlayerStateAddress + 0x318);
                                                int PlayerNameLengthPlus1 = (int)Memory.ZwReadInt64(processHandle, (IntPtr)PlayerStateAddress + 0x318 + 0x8);

                                                playername = Memory.ZwReadString(processHandle, (IntPtr)PlayerNameAddress, true, PlayerNameLengthPlus1);


                                                if (!killername.Equals("Victor"))
                                                {
                                                    if (Components.VisualsComponent.DrawKillerLine.Enabled && dist > 5)
                                                    {
                                                        //Renderer.DrawFPSBox(vScreen_h3adKiller, vScreen_f33tKiller, Components.VisualsComponent.KillerColor.Color, BoxStance.standing, Components.VisualsComponent.DrawBoxThic.Value, Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                        Renderer.DrawLine(GameCenterPos2, vScreen_h3adKiller, Components.VisualsComponent.KillerColor.Color, Components.VisualsComponent.DrawBoxThic.Value);
                                                        Renderer.DrawText(killername + " [" + dist + "m]", vScreen_f33tKiller.X, vScreen_f33tKiller.Y + 5, Components.VisualsComponent.KillerColor.Color, 14, TextAlignment.centered, false);
                                                        Renderer.DrawText(playername, vScreen_f33tKiller.X, vScreen_f33tKiller.Y + 16, Components.VisualsComponent.KillerColor.Color, 14, TextAlignment.centered, false);

                                                    }


                                                    if (Components.VisualsComponent.DrawKillerBox.Enabled && dist > 5)
                                                    {
                                                        Renderer.DrawFPSBox(vScreen_h3adKiller, vScreen_f33tKiller, Components.VisualsComponent.KillerColor.Color, BoxStance.standing, Components.VisualsComponent.DrawBoxThic.Value, Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                        Renderer.DrawText(killername + " [" + dist + "m]", vScreen_f33tKiller.X, vScreen_f33tKiller.Y + 5, Components.VisualsComponent.KillerColor.Color, 14, TextAlignment.centered, false);
                                                        Renderer.DrawText(playername, vScreen_f33tKiller.X, vScreen_f33tKiller.Y + 16, Components.VisualsComponent.KillerColor.Color, 14, TextAlignment.centered, false);

                                                    }
                                                }
                                                else
                                                { Renderer.DrawText(killername + " [" + dist + "m]", vScreen_f33tKiller.X, vScreen_f33tKiller.Y + 5, Components.VisualsComponent.KillerColor.Color, 14, TextAlignment.centered, false); }

                                                var AimDist2D = GetDistance2D(vScreen_h3adKiller, GameCenterPos);
                                                if (Components.AimbotComponentSurvivor.AimFov.Value < AimDist2D) continue;

                                                if (AimDist2D < fClosestPos)
                                                {
                                                    fClosestPos = AimDist2D;
                                                    AimTarg2D = vScreen_h3adKiller;

                                                    if (Components.AimbotComponentSurvivor.AimKey.Enabled && Components.AimbotComponentSurvivor.AimGlobalBool.Enabled && dist <= 30)
                                                    {
                                                        double DistX = 0;
                                                        double DistY = 0;
                                                        DistX = (AimTarg2D.X) - GameCenterPos.X;
                                                        DistY = (AimTarg2D.Y) - GameCenterPos.Y;

                                                        double slowDistX = DistX / (1.0f + (Math.Abs(DistX) / (1.0f + Components.AimbotComponentSurvivor.AimSpeed.Value)));
                                                        double slowDistY = DistY / (1.0f + (Math.Abs(DistY) / (1.0f + Components.AimbotComponentSurvivor.AimSpeed.Value)));
                                                        Input.mouse_eventWS(MouseEventFlags.MOVE, (int)slowDistX, (int)slowDistY, MouseEventDataXButtons.NONE, IntPtr.Zero);
                                                        //Vector3 Aimassist = new Vector3()
                                                    }

                                                }
                                            }
                                        }

                                        /*if (AActorID == survivorID && AActorID == Pallet && AActorID == killerID && dist < 4)
                                        {
                                            if (Components.AimbotComponentSurvivor.AutoPallet.Enabled)
                                            {
                                                Input.KeyPress(VirtualKeyCode.Space);
                                            }
                                        }*/

                                        if (Components.VisualsComponent.DrawLocker.Enabled)
                                        {
                                            if (AActorID == Locker && dist < 50)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    var IsInBasement = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x0308);
                                                    if (dist < 100 && IsInBasement == false)//isinbasement doesnt work on lockers i guess?????
                                                    {
                                                        Renderer.DrawText("Locker [" + dist + "m]", vScreen_d3d11, Color.Indigo, 12);
                                                    }
                                                }
                                            }
                                        }

                                        if (Components.VisualsComponent.DrawItems.Enabled)
                                        {
                                            if (dist > 5)
                                            {
                                                if (AActorID == MedKit) //not working??? //working indeed
                                                {
                                                    Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                    if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                    {
                                                        Renderer.DrawText(" MedKit [" + dist + "m]", vScreen_d3d11, Color.HotPink, 12, TextAlignment.centered, false);
                                                    }
                                                }
                                                if (AActorID == ToolBox)
                                                {
                                                    Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                    if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                    {
                                                        Renderer.DrawText("ToolBox [" + dist + "m]", vScreen_d3d11, Color.DarkOrchid, 12, TextAlignment.centered, false);
                                                    }
                                                }
                                                if (AActorID == Key)
                                                {
                                                    Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                    if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                    {

                                                        Renderer.DrawText("Key [" + dist + "m]", vScreen_d3d11, Color.Firebrick, 12);
                                                    }
                                                }
                                                if (AActorID == Flashlight)
                                                {
                                                    Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                    if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                    {
                                                        Renderer.DrawText("Flashlight [" + dist + "m]", vScreen_d3d11, Color.DarkOrchid, 12, TextAlignment.centered, false);
                                                    }
                                                }
                                            }
                                        }

                                        if (Components.VisualsComponent.DrawHook.Enabled)
                                        {
                                            if (AActorID == Hook)
                                            {
                                                var IsInBasement = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x0308);

                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    if (dist < 100 && IsInBasement == false)
                                                        Renderer.DrawText("Hook [" + dist + "m]", vScreen_d3d11, Color.DeepSkyBlue, 12, TextAlignment.centered, false);
                                                }
                                            }
                                        }


                                        if (Components.VisualsComponent.DrawPallet.Enabled) // not working???
                                        {
                                            if (AActorID == Pallet)
                                            {
                                                var state = Memory.ZwReadByte(processHandle, (IntPtr)AActor.ToInt64() + 0x02DC);


                                                Vector2 vScreen_d3d11bb = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11bb, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    if (dist < 100 && state == 0)
                                                        Renderer.DrawText("Pallet [" + dist + "m]", vScreen_d3d11bb, Color.AliceBlue, 12, TextAlignment.centered, false);

                                                }
                                            }
                                        }

                                        if (Components.VisualsComponent.DrawChest.Enabled)
                                        {
                                            if (AActorID == Chest)
                                            {
                                                var HasBeenSearched = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x030C);


                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    if (HasBeenSearched == false)
                                                        Renderer.DrawText("Chest [" + dist + "m]", vScreen_d3d11, Color.Purple, 12, TextAlignment.centered, false);
                                                }
                                            }

                                        }

                                        if (Components.VisualsComponent.DrawTrap.Enabled)
                                        {
                                            if (AActorID == Trap)
                                            {
                                                var IsTrapSet = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x0420);

                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    if (IsTrapSet == true && dist < 200)
                                                        Renderer.DrawText("Trap [" + dist + "m]", vScreen_d3d11, Color.Maroon, 16, TextAlignment.centered, false);
                                                }
                                            }
                                            if (AActorID == HagTrap)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    Renderer.DrawText("Trap [" + dist + "m]", vScreen_d3d11, Color.Maroon, 16, TextAlignment.centered, false);
                                                }
                                            }
                                            if (AActorID == Portal)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    Renderer.DrawText("Portal [" + dist + "m]", vScreen_d3d11, Color.Maroon, 12, TextAlignment.centered, false);
                                                }
                                            }
                                            if (AActorID == DreamSnare)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    Renderer.DrawText("Snare [" + dist + "m]", vScreen_d3d11, Color.Maroon, 16, TextAlignment.centered, false);
                                                }
                                            }
                                            if (AActorID == Jigsaw)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    Renderer.DrawText("JigSaw Box [" + dist + "m]", vScreen_d3d11, Color.Maroon, 12);
                                                }
                                            }
                                        }


                                        if (Components.VisualsComponent.DrawMiscInfo.Enabled)
                                        {

                                            if (AActorID == totemID)
                                            {

                                                var IsCleansed = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x02EC);
                                                var HexID = Memory.ZwReadUInt32(processHandle, (IntPtr)(AActor.ToInt64() + 0x02E0));
                                                var HexID2 = GetNameFromID(HexID);

                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    if (IsCleansed == false)
                                                    {
                                                        if (!HexID2.Contains("None"))
                                                        {
                                                            string hex = HexID2;
                                                            if (HexID2.ToLower().Contains("favor")) { hex = "HEX: Blood Favour "; }
                                                            if (HexID2.ToLower().Contains("devour")) { hex = "HEX: Devour Hope "; }
                                                            if (HexID2.ToLower().Contains("huntress")) { hex = "HEX: Huntress Lullaby "; }
                                                            if (HexID2.ToLower().Contains("haunted")) { hex = "HEX: Haunted Ground "; }
                                                            if (HexID2.ToLower().Contains("no_one")) { hex = "HEX: NOED "; }
                                                            if (HexID2.ToLower().Contains("retribution")) { hex = "HEX: Retribution "; }
                                                            if (HexID2.ToLower().Contains("ruin")) { hex = "HEX: Ruin "; }
                                                            if (HexID2.ToLower().Contains("the_third")) { hex = "HEX: The Third Seal "; }
                                                            if (HexID2.ToLower().Contains("thrill_of")) { hex = "HEX: Thrill of The Hunt "; }
                                                            if (HexID2.ToLower().Contains("undying")) { hex = "HEX: Undying "; }

                                                            Renderer.DrawText(" Totem [" + hex + dist + " m]", vScreen_d3d11, Color.HotPink, 12, TextAlignment.centered, false);
                                                        }
                                                        else
                                                        {
                                                            if (!Components.VisualsComponent.DrawOnlyHex.Enabled)
                                                            {
                                                                Renderer.DrawText(" Totem [" + dist + " m]", vScreen_d3d11, Color.WhiteSmoke, 12, TextAlignment.centered, false);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            if (AActorID == escapeID)
                                            {
                                                //var Hatchstate = Memory.ZwReadByte(processHandle, (IntPtr)AActor.ToInt64() + 0x02E8);
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    Renderer.DrawText("Escape [" + dist + "m]", vScreen_d3d11.X, vScreen_d3d11.Y - 10, Color.Orange, 12, TextAlignment.centered, false);
                                                }
                                            }
                                            if (AActorID == hatchID)
                                            {
                                                //var Hatchstate = Memory.ZwReadByte(processHandle, (IntPtr)AActor.ToInt64() + 0x02E8);
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    Renderer.DrawText("Hatch [" + dist + "m]", vScreen_d3d11, Color.Orange, 12, TextAlignment.centered, false);
                                                }
                                            }


                                        }

                                        if (Components.VisualsComponent.DrawGenerators.Enabled)
                                        {
                                            if (AActorID != generatorID)
                                                continue;
                                            var isRepaired = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x02E1);
                                            var isBlocked = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x03A4);


                                            var currentProgressPercent = Memory.ZwReadFloat(processHandle, (IntPtr)AActor.ToInt64() + 0x02F0) * 100;
                                            Color selectedColor;
                                            if (isBlocked)
                                                selectedColor = Color.Red;

                                            else
                                                selectedColor = Color.Yellow;
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                if (isRepaired == false)
                                                    Renderer.DrawText($"Generator [{dist}m] ({currentProgressPercent:##0}%)", vScreen_d3d11, selectedColor, 14, TextAlignment.centered, false);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}