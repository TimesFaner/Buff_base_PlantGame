using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QFramework
{
    public class AudioPlayer : IPoolable, IPoolType
    {
        private AudioClip mAudioClip;
        private int mCustomEventID;

        private bool mIsLoop;
        private bool mIsPause;
        private float mLeftDelayTime = -1;
        private IAudioLoader mLoader;
        private Action<AudioPlayer> mOnFinishListener;

        private Action<AudioPlayer> mOnStartListener;
        private TimeItem mTimeItem;
        private bool mUsedCache = true;

        public string Name { get; private set; }

        public AudioSource AudioSource { get; private set; }

        public int customEventID
        {
            get => mCustomEventID;
            set => mCustomEventID = -1;
        }

        public bool usedCache
        {
            get => mUsedCache;
            set => mUsedCache = false;
        }

        public int playCount { get; private set; }

        public bool IsRecycled { get; set; } = false;

        public void OnRecycled()
        {
            CleanResources();
        }

        public void Recycle2Cache()
        {
            if (!SafeObjectPool<AudioPlayer>.Instance.Recycle(this))
                if (AudioSource != null)
                {
                    Object.Destroy(AudioSource);
                    AudioSource = null;
                }
        }

        public static AudioPlayer Allocate()
        {
            return SafeObjectPool<AudioPlayer>.Instance.Allocate();
        }

        public void SetOnStartListener(Action<AudioPlayer> l)
        {
            mOnStartListener = l;
        }

        public void SetOnFinishListener(Action<AudioPlayer> l)
        {
            mOnFinishListener = l;
        }

        public void SetAudioExt(GameObject root, AudioClip clip, string name, bool loop)
        {
            if (clip == null || Name == name) return;

            if (AudioSource == null) AudioSource = root.AddComponent<AudioSource>();

            CleanResources();

            mIsLoop = loop;
            Name = name;

            mAudioClip = clip;
            PlayAudioClip();
        }

        public void SetAudio(GameObject root, string name, bool loop)
        {
            if (string.IsNullOrEmpty(name)) return;

            if (Name == name) return;

            if (AudioSource == null) AudioSource = root.AddComponent<AudioSource>();

            //防止卸载后立马加载的情况
            var preLoader = mLoader;

            mLoader = null;

            CleanResources();

            mLoader = AudioKit.Config.AudioLoaderPool.AllocateLoader();

            mIsLoop = loop;
            Name = name;

            var keys = AudioSearchKeys.Allocate();
            keys.AssetName = name;
            mLoader.LoadClipAsync(keys, OnResLoadFinish);
            keys.Recycle2Cache();

            if (preLoader != null)
            {
                preLoader.Unload();
                AudioKit.Config.AudioLoaderPool.RecycleLoader(preLoader);
                preLoader = null;
            }

            // if (mLoader != null)
            // {
            //     mLoader.LoadAsync();
            // }
        }

        public void Stop()
        {
            Release();
        }

        public void Pause()
        {
            if (mIsPause) return;

            mLeftDelayTime = -1;
            //暂停
            if (mTimeItem != null)
            {
                mLeftDelayTime = mTimeItem.SortScore - Timer.Instance.currentScaleTime;
                mTimeItem.Cancel();
                mTimeItem = null;
            }

            mIsPause = true;

            AudioSource.Pause();
        }

        public void Resume()
        {
            if (!mIsPause) return;

            if (mLeftDelayTime >= 0) mTimeItem = Timer.Instance.Post2Scale(OnResumeTimeTick, mLeftDelayTime);

            mIsPause = false;

            AudioSource.Play();
        }

        public void SetVolume(float volume)
        {
            if (null != AudioSource) AudioSource.volume = volume;
        }

        private void OnResLoadFinish(bool result, AudioClip clip)
        {
            if (!result)
            {
                Release();
                return;
            }

            mAudioClip = clip;

            if (mAudioClip == null)
            {
                Debug.LogError("Asset Is Invalid AudioClip:" + Name);
                Release();
                return;
            }

            PlayAudioClip();
        }

        private void PlayAudioClip()
        {
            if (AudioSource == null || mAudioClip == null)
            {
                Release();
                return;
            }

            AudioSource.clip = mAudioClip;
            AudioSource.loop = mIsLoop;
            AudioSource.volume = 1.0f;

            var loopCount = 1;
            if (mIsLoop) loopCount = -1;

            mTimeItem = Timer.Instance.Post2Scale(OnSoundPlayFinish, mAudioClip.length, loopCount);

            if (null != mOnStartListener) mOnStartListener(this);

            AudioSource.Play();
        }

        private void OnResumeTimeTick(int repeatCount)
        {
            OnSoundPlayFinish(repeatCount);

            if (mIsLoop) mTimeItem = Timer.Instance.Post2Scale(OnSoundPlayFinish, mAudioClip.length, -1);
        }

        private void OnSoundPlayFinish(int count)
        {
            ++playCount;

            if (mOnFinishListener != null) mOnFinishListener(this);

            if (mCustomEventID > 0)
            {
                // QEventSystem.Instance.Send(mCustomEventID, this);
            }

            if (!mIsLoop) Release();
        }

        private void Release()
        {
            CleanResources();

            if (mUsedCache) Recycle2Cache();
        }

        private void CleanResources()
        {
            Name = null;

            playCount = 0;
            mIsPause = false;
            mOnFinishListener = null;
            mLeftDelayTime = -1;
            mCustomEventID = -1;

            if (mTimeItem != null)
            {
                mTimeItem.Cancel();
                mTimeItem = null;
            }

            if (AudioSource)
                if (AudioSource.clip == mAudioClip)
                {
                    AudioSource.Stop();
                    AudioSource.clip = null;
                }

            mAudioClip = null;

            if (mLoader != null)
            {
                mLoader.Unload();
                AudioKit.Config.AudioLoaderPool.RecycleLoader(mLoader);
                mLoader = null;
            }
        }
    }
}