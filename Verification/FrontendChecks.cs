using System;
using System.IO;
using Poolrooms.Core;

static partial class Checks
{
    static void FrontendContract()
    {
        for(int iteration=0;iteration<1000;iteration++)
        {
            var flow=new FrontendFlow();
            Check(flow.Visible&&!flow.HasSession&&!flow.CanResume,"Frontend begins with a live game");
            Check(!flow.Resume()&&!flow.Suspend(),"Menu pretends an absent session exists");
            foreach(FrontendPage page in Enum.GetValues(typeof(FrontendPage)))
                Check(flow.Navigate(page)&&flow.Page==page&&!flow.HasSession,"Browsing a page creates a session");
            Check(!flow.Navigate((FrontendPage)100),"Unknown page accepted");
            int cancelled=flow.Begin();Check(cancelled>=0,"Loading did not start");
            Check(flow.Begin()==-1&&!flow.Navigate(FrontendPage.Home),"Double activation creates competing journeys");
            Check(flow.Fail(cancelled,"Cancelled")&&!flow.Complete(cancelled)&&!flow.CanResume,"Cancelled loader regains control");
            int active=flow.Begin();Check(active>cancelled&&!flow.Complete(cancelled),"Old loading ticket survives a restart");
            Check(flow.Complete(active)&&flow.HasSession&&!flow.Visible,"Ready game not activated");
            Check(!flow.Navigate(FrontendPage.Home),"Game switches menu page without suspension");
            Check(flow.Suspend()&&flow.CanResume&&flow.Visible,"Return to title loses resumability");
            foreach(FrontendPage page in Enum.GetValues(typeof(FrontendPage)))Check(flow.Navigate(page)&&flow.CanResume,"Settings destroy current session");
            Check(flow.Resume()&&!flow.Visible&&flow.HasSession,"Continue replaces rather than resumes session");
            Check(!flow.Complete(active)&&!flow.Fail(active,"stale"),"Finished loader can still mutate game state");
        }
        foreach(string text in new[]{"", "0", "-2147483648", "2147483647", "Una tarde", "Colinas cálidas"})
        {
            Check(FrontendFlow.TrySeed(text,87,out int first)&&FrontendFlow.TrySeed(text,87,out int second)&&first==second,"Seed is not reproducible");
        }
        Check(!FrontendFlow.TrySeed(new string('x',65),0,out _),"Unbounded seed text accepted");
        Check(FrontendFlow.TrySeed("",87,out int fallback)&&fallback==87,"Empty seed does not preserve selected seed");
        float previous=0;
        for(int frame=0;frame<100;frame++)
        {
            float alpha=FrontendFlow.Fade(frame*.01f,false);
            Check(alpha>=previous&&alpha>=0&&alpha<=1,"Background crossfade is not bounded and monotone");previous=alpha;
        }
        Check(FrontendFlow.Fade(0,true)==1&&FrontendFlow.Fade(1,false)==1,"Reduced motion or complete transition broken");
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string Read(string file)=>File.ReadAllText(Path.Combine(root,"Assets/PoolroomsProcedural/"+file));
        string frontend=Read("Runtime/PoolroomsFrontend.cs"),ui=Read("Runtime/PoolroomsFrontend.UI.cs");
        string world=Read("Runtime/PoolroomsWorld.cs"),pause=Read("Runtime/PoolroomsPauseMenu.cs");
        Check(frontend.Contains("RuntimeInitializeLoadType.BeforeSceneLoad")&&frontend.Contains("SceneManager.sceneLoaded+=CaptureLoadedDemo"),"Existing demo never enters independent frontend");
        Check(frontend.Contains("dormant.SetActive(false)")&&frontend.Contains("SceneManager.UnloadSceneAsync(scene)"),"Old demo remains running behind the menu");
        Check(frontend.Contains("template=journeyTemplate")&&!frontend.Contains("template=Instantiate(journeyTemplate)"),"Explicit menu instantiates gameplay before selection");
        Check(frontend.Contains("session=Instantiate(template)")&&frontend.Contains("actor.showStartMenu=false"),"Explicit play missing or legacy overlay still opens");
        Check(frontend.Contains("world.VisitEnvironment")&&frontend.Contains("world.VisitJourneyDistrict")&&!frontend.Contains("VisitAdministratorDistrict"),"Normal game selection depends on master mode");
        Check(frontend.Contains("world.FrontendSuspended=true")&&pause.Contains("ReturnToMenu()")&&world.Contains("if(FrontendSuspended)"),"Return to title does not suspend streaming");
        Check(ui.Contains("ReleaseLeavingArt")&&ui.Contains("Time.unscaledDeltaTime")&&ui.Contains("ScaleMode.ScaleAndCrop"),"Background transition lifetime/scaling missing");
        Check(Read("Runtime/PoolroomsFrontend.Options.cs").Contains("ApplyFrontendPreferences")&&Read("Runtime/PoolroomsGameSettings.cs").Contains("Preferences=Copy(values)"),"Title settings are discarded by current game");
        Console.WriteLine("Frontend: 1000 lifecycle/cancel/restart/resume sequences, six page routes, deterministic seeds, bounded reduced-motion crossfade and integration contracts passed. Native scenes/UI/input/rendering unrun.");
    }
}
