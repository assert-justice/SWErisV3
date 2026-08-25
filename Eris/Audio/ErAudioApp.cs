using MiniAudioEx.Core.StandardAPI;

namespace Eris.Audio;

public class ErAudioApp
{
    public ErAudioApp()
    {
        //
    }
    public void Cleanup()
    {
        AudioContext.Deinitialize();
    }
    public void Init()
    {
        AudioContext.Initialize(44100, 2, 2048);
    }
    public void Update()
    {
        AudioContext.Update();
    }
}
