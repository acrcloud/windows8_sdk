using System;
using System.IO;

using System.Collections.Generic;

using System.Net.Http;
using System.Diagnostics;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.Media.MediaProperties;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Capture;
using System.Threading.Tasks;

using Windows.System.Threading;
using Windows.Foundation;

using Windows.Data.Json;

using Windows.UI.Xaml.Controls;
using Windows.UI.Core;


using ACRCloudExtrDllCOM;

namespace ACRCloud
{
    public class ACRCloudRecognizer
    {
        private string mHost = "";
        private string mAccessKey = "";
        private string mAccessSecret = "";
        private int mTimeout = 5 * 1000; // ms

        private ACRCloudExtrTool mACRCloudExtrTool = new ACRCloudExtrTool();

        public ACRCloudRecognizer(IDictionary<string, Object> config)
        {
            if (config == null) return;

            if (config.ContainsKey("host"))
            {
                this.mHost = (string)config["host"];
            }
            if (config.ContainsKey("access_key"))
            {
                this.mAccessKey = (string)config["access_key"];
            }
            if (config.ContainsKey("access_secret"))
            {
                this.mAccessSecret = (string)config["access_secret"];
            }
            if (config.ContainsKey("timeout"))
            {
                this.mTimeout = 1000 * (int)config["timeout"];
            }
        }

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
        { 
            if (wavAudioBuffer.Length != wavAudioBufferLen)
            {
                byte[] tmp = new byte[wavAudioBuffer.Length];
                wavAudioBuffer.CopyTo(tmp, 0);
                wavAudioBuffer = tmp;
            }

            byte[] fp = null;
            mACRCloudExtrTool.CreateFingerprint(wavAudioBuffer, out fp);
            if (fp == null)
            {
                return ACRCloudStatusCode.NO_RESULT;
            }
            return this.DoRecognize(fp);
        }

        private string PostHttp(string url, IDictionary<string, Object> postParams)
        {
            string res = "";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(mTimeout);
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        foreach (var item in postParams)
                        {
                            if (item.Value is string)
                            {
                                HttpContent strPost = new StringContent((string)item.Value);
                                strPost.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
                                content.Add(strPost, "\"" + item.Key + "\"");
                            }
                            else if (item.Value is byte[])
                            {
                                byte[] sample = (byte[])item.Value;
                                HttpContent streamPost = new ByteArrayContent(sample);
                                streamPost.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/octet-stream");
                                content.Add(streamPost, "\"sample\"", "\"sample.ef\"");
                            }
                        }
                        var response = client.PostAsync(url, content).Result;
                        res = response.Content.ReadAsStringAsync().Result;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                res = ACRCloudStatusCode.HTTP_ERROR;
            }
            return res;
        }

        public string EncryptByHMACSHA1(string input, string key)
        {
            IBuffer keyBuffer = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            IBuffer inputBuffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            MacAlgorithmProvider objMacProvider = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA1");
            CryptographicKey hmacKey = objMacProvider.CreateKey(keyBuffer);
            IBuffer buffHmac = CryptographicEngine.SignHashedData(hmacKey, inputBuffer);
            return EncodeToBase64(WindowsRuntimeBufferExtensions.ToArray(buffHmac));
        }

        public string EncodeToBase64(byte[] input)
        {
            string res = Convert.ToBase64String(input, 0, input.Length);
            return res;
        }

        private string DoRecognize(byte[] queryData)
        {
            string method = "POST";
            string httpURL = "/v1/identify";
            string dataType = "fingerprint";
            string sigVersion = "1";
            string timestamp = ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();

            string reqURL = "http://" + mHost + httpURL;

            string sigStr = method + "\n" + httpURL + "\n" + mAccessKey + "\n" + dataType + "\n" + sigVersion + "\n" + timestamp;
            string signature = EncryptByHMACSHA1(sigStr, this.mAccessSecret);

            var postParams = new Dictionary<string, object>();
            postParams.Add("access_key", this.mAccessKey);
            postParams.Add("sample_bytes", queryData.Length.ToString());
            postParams.Add("sample", queryData);
            postParams.Add("timestamp", timestamp);
            postParams.Add("signature", signature);
            postParams.Add("data_type", dataType);
            postParams.Add("signature_version", sigVersion);

            string res = PostHttp(reqURL, postParams);

            return res;
        }
    }

    public class ACRCloudStatusCode
    {
        public static string HTTP_ERROR = "{\"status\":{\"msg\":\"Http Error\", \"code\":3000}}";
        public static string NO_RESULT = "{\"status\":{\"msg\":\"No Result\", \"code\":1001}}";
        public static string GEN_FP_ERROR = "{\"status\":{\"msg\":\"Gen Fingerprint Error\", \"code\":2004}}";
        public static string RECORD_ERROR = "{\"status\":{\"msg\":\"Record Error\", \"code\":2000}}";
        public static string JSON_ERROR = "{\"status\":{\"msg\":\"json error\", \"code\":2002}}";
    }

    public class ACRCloudRecorder
    {
        private MediaCapture mMediaCapture = null;
        private InMemoryRandomAccessStream mRecorderStream = null;

        private bool mIsRecording = false;

        private static ACRCloudRecorder mACRCloudRecorderInstance = new ACRCloudRecorder();

        private ACRCloudRecorder()
        {            
        }

        public static ACRCloudRecorder GetInstance()
        {
            return mACRCloudRecorderInstance;
        }

        private async Task<bool> Init()
        {
            if (mRecorderStream != null)
            {
                mRecorderStream.Dispose();
            }
            mRecorderStream = new InMemoryRandomAccessStream();
            if (mMediaCapture != null)
            {
                mMediaCapture.Dispose();
            }
            try
            {
                mMediaCapture = new MediaCapture();
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio
                };
                await mMediaCapture.InitializeAsync(settings);
                mMediaCapture.RecordLimitationExceeded += (MediaCapture sender) =>
                {
                    Stop();
                    Debug.WriteLine("RecordLimitationExceeded");
                    throw new Exception("RecordLimitationExceeded");
                };
                mMediaCapture.Failed += (MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs) =>
                {
                    Debug.WriteLine("failed");
                    mIsRecording = false;
                    throw new Exception("Failed");
                };
            } catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            
            return true;
        }

        public async Task<bool> Start()
        {
            Stop();
            bool res = await Init();
            if (!res) return false;

            MediaEncodingProfile outProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto);
            outProfile.Audio = AudioEncodingProperties.CreatePcm(8000, 1, 16);
            await mMediaCapture.StartRecordToStreamAsync(outProfile, mRecorderStream);
            if (res) this.mIsRecording = true;

            return true;
        }

        public async void Stop()
        {
            if (this.mIsRecording)
            {
                await mMediaCapture.StopRecordAsync();
                this.mIsRecording = false;
            }
        }

        public bool isRecording()
        {
            return this.mIsRecording;
        }

        public async Task<byte[]> GetCurrentAudio()
        {
            if (this.mRecorderStream == null || this.mRecorderStream.Size <= 0) return null;

            IRandomAccessStream cloneAudioStream = mRecorderStream.CloneStream();
            byte[] aa = new byte[cloneAudioStream.Size];
            int asize = await cloneAudioStream.AsStream().ReadAsync(aa, 0, aa.Length);
            cloneAudioStream.Dispose();

            return aa;
        }
    }

    public interface IACRCloudClientListener
    {
        /**
          *
          *  callback function of ACRCloudWorker.
          *
          **/
        void OnResult(string result);
    }

    public class ACRCloudClient : Page
    {

        private IACRCloudClientListener mListener = null;
        private ACRCloudRecorder mRecorder = null;
        private ACRCloudRecognizer mRecognizer = null;

        private int mRecognizeInterval = 3; // seconds
        private int mMaxRecognizeAudioTime = 12;
        private int mHttpErrorRetryNum = 3;

        private bool mIsRunning = false;

        private IAsyncAction mThreadPoolHandler = null;


        public ACRCloudClient(IACRCloudClientListener lins, IDictionary<string, Object> config)
        {
            this.mListener = lins;
            this.mRecorder = ACRCloudRecorder.GetInstance();
            this.mRecognizer = new ACRCloudRecognizer(config);
        }

        /**
          *
          *  Cancel this recognition Session.
          * 
          *  Note:  ACRCloudWorker do not callback OnResult.
          * 
          **/
        public void Cancel()
        {
            this.mIsRunning = false;
        }

        /**
          *
          *  Start a Thread to recognize.
          * 
          **/
        public void Start()
        {
            mThreadPoolHandler = ThreadPool.RunAsync(delegate { Run(); });            
        }

        /**
          *
          *  ACRCloud Worker main function.
          * 
          *    Every (mRecognizeInterval) seconds recognize ACRCloud Server by audio buffer. 
          *  If has result and callback OnResult.
          *
          **/
        private async void Run()
        {
            if (this.mRecorder == null || this.mListener == null)
            {
                return;
            }

            bool recordStatus = await this.mRecorder.Start();
            if (!recordStatus)
            {
                this.mListener.OnResult(ACRCloudStatusCode.RECORD_ERROR);
                return;
            }

            this.mIsRunning = true;

            string result = "";
            int retryNum = this.mHttpErrorRetryNum;
            int nextRecognizeTime = 3; // seconds
            int recordRetryNum = 2;
            int oldRecordAudioBufferLen = 0;
            while (this.mIsRunning)
            {
                await Task.Delay(1000);
                if (!this.mIsRunning)
                {
                    break;
                }

                byte[] pcmData = await this.mRecorder.GetCurrentAudio();

                if (pcmData == null || pcmData.Length == oldRecordAudioBufferLen)
                { // check microphone is OK
                    recordRetryNum--;
                    if (recordRetryNum <= 0)
                    {
                        if (result == "")
                        {
                            result = ACRCloudStatusCode.RECORD_ERROR;
                        }
                        break;
                    }
                    continue;
                }
                recordRetryNum = 3;
                oldRecordAudioBufferLen = pcmData.Length;

                if (pcmData.Length >= nextRecognizeTime * 2 * 8000)
                {
                    result = this.DoRecognize(pcmData);
                    Debug.WriteLine(result);
                    if (result == "")
                    {
                        retryNum--;
                        result = ACRCloudStatusCode.HTTP_ERROR;
                        if (retryNum <= 0)
                        {
                            break;
                        }
                        continue;
                    }
                    retryNum = this.mHttpErrorRetryNum;

                    if (result != null)
                    {
                        try
                        {
                            JsonValue rTmp = JsonValue.Parse(result);
                            JsonObject jRoot = rTmp.GetObject();
                            JsonObject status = jRoot.GetNamedObject("status");
                            if ((int)status.GetNamedNumber("code") != 1001)
                            {
                                break;
                            }
                            if (nextRecognizeTime >= this.mMaxRecognizeAudioTime)
                            {
                                if (result == "" || result == null)
                                {
                                    result = ACRCloudStatusCode.NO_RESULT;
                                }
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                            result = ACRCloudStatusCode.JSON_ERROR;
                            break;
                        }
                    }

                    nextRecognizeTime = pcmData.Length / (2 * 8000) + this.mRecognizeInterval;
                    if (nextRecognizeTime > this.mMaxRecognizeAudioTime)
                    {
                        nextRecognizeTime = this.mMaxRecognizeAudioTime;
                    }
                }
            }           

            if (this.mIsRunning && this.mListener != null)
            {
                this.mRecorder.Stop();
                OnResult(result);
            }

            this.mIsRunning = false;
        }
      
        private async void OnResult(string result)
        {
            await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    new DispatchedHandler(() => {

                        this.mListener.OnResult(result);

                    }
                ));
        }

        /**
          *
          *  Recognize ACRCloud Server by audio buffer.
          * 
          **/
        private string DoRecognize(byte[] pcmBuffer)
        {
            int pcmBufferLen = pcmBuffer.Length;

            return this.mRecognizer.Recognize(pcmBuffer, pcmBufferLen);
        }
    }
}