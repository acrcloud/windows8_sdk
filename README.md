# Audio Recognition Universal Windowphone 8.1 SDK

## Overview
  [ACRCloud](https://www.acrcloud.com/) provides cloud [Automatic Content Recognition](https://www.acrcloud.com/docs/introduction/automatic-content-recognition/) services for [Audio Fingerprinting](https://www.acrcloud.com/docs/introduction/audio-fingerprinting/) based applications such as **[Audio Recognition](https://www.acrcloud.com/music-recognition)** (supports music, video, ads for both online and offline), **[Broadcast Monitoring](https://www.acrcloud.com/broadcast-monitoring)**, **[Second Screen](https://www.acrcloud.com/second-screen-synchronization)**, **[Copyright Protection](https://www.acrcloud.com/copyright-protection-de-duplication)** and etc.<br>
  
  This SDK support record by microphone, and you can run it on **Winphone(ARM)**. 

## Requirements
Follow one of the tutorials to create a project and get your host, access_key and access_secret.

 * [How to identify songs by sound](https://www.acrcloud.com/docs/tutorials/identify-music-by-sound/)
 
 * [How to detect custom audio content by sound](https://www.acrcloud.com/docs/tutorials/identify-audio-custom-content/)
 
## Functions
Introduction all API.
### WindowsRuntimeComponent_ACRCloudExtrTool
```c
     class ACRCloudExtrTool {
          public void CreateFingerprint(byte[] pcmBuffer, out byte[] fp);
           /**
            *
            *  create "ACRCloud Fingerprint" by wav audio buffer(RIFF (little-endian) data, WAVE audio, Microsoft PCM, 16 bit, mono 8000 Hz) 
            *
            *  @param pcmBuffer query audio buffer
            *  @param fp out fingerprint
            *  
            *
            **/ 
     }
```

### ACRCloudSDK.cs
```c
public class ACRCloudRecognizer
{
    /**
          *
          *  recognize by wav audio buffer(RIFF (little-endian) data, WAVE audio, Microsoft PCM, 16 bit, mono 8000 Hz) 
          *
          *  @param wavAudioBuffer query audio buffer
          *  @param wavAudioBufferLen the length of wavAudioBuffer
          *  
          *  @return result 
          *
          **/
        public string Recognize(byte[] wavAudioBuffer, int wavAudioBufferLen)
}

public class ACRCloudRecorder
{
}

public class ACRCloudClient : Page
{
}
```
## Example
run the project ACRCloudWinPhoneSDK: <br>
```c
	private void stopbtn_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                client.Cancel();
            }
            resultTextBlock.Text = "stoped";
        }

        private void startbtn_Click(object sender, RoutedEventArgs e)
        {
            var config = new Dictionary<string, object>();

            // Replace "XXXXXXXX" below with your project's access_key and access_secret
            config.Add("host", "XXXXXXXX");            
            config.Add("access_key", "XXXXXXXX");
            config.Add("access_secret", "XXXXXXXX");

            client = new ACRCloudClient(this, config);
            client.Start();

            resultTextBlock.Text = "recording";
        }

        void IACRCloudClientListener.OnResult(string result)
        {
            resultTextBlock.Text = result;
        }
```
