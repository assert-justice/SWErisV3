using MiniAudioEx.Core.StandardAPI;

namespace Eris.Audio;

public class ErAudioSource
{
    // public readonly int Handle;
    private readonly ErAudioApp AudioApp;
    private readonly AudioSource Source;
    private List<AudioClip> Clips = [];
    public ErAudioSource(ErAudioApp audioApp)
    {
        AudioApp = audioApp;
        Source = new();
    }
    public void PlayFile(string filepath)
    {
        AudioClip clip = new(filepath);
        Clips.Add(clip);
        Source.Play(clip);
    }
}