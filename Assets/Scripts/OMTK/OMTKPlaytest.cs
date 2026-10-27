using System.Collections;
using System.Reflection;
using UnityEngine;

namespace OMTK
{
    public class OMTKPlaytest : MonoBehaviour
    {
        private OMTKGame game;
        private FieldInfo tokenField;
        private FieldInfo enemiesField;
        private FieldInfo finaleFacingField;
        private FieldInfo playerYawField;
        private GameState lastState = (GameState)(-1);
        private OMTKEnemy lastToken;
        private bool finaleSwiped;
        private bool finished;
        private float runStartedAt;

        private void Start()
        {
            game = Object.FindAnyObjectByType<OMTKGame>();
            if (game == null)
            {
                Debug.LogError("[PLAYTEST] no OMTKGame");
                enabled = false;
                return;
            }

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            tokenField = typeof(OMTKGame).GetField("token", flags);
            enemiesField = typeof(OMTKGame).GetField("enemies", flags);
            finaleFacingField = typeof(OMTKGame).GetField("finaleFacing", flags);
            playerYawField = typeof(OMTKGame).GetField("<PlayerYaw>k__BackingField", flags);
            runStartedAt = Time.unscaledTime;
            Time.timeScale = 2f;
            Debug.Log("[PLAYTEST] started");
        }

        private void Update()
        {
            if (finished || game == null)
            {
                return;
            }

            if (game.State != lastState)
            {
                lastState = game.State;
                Debug.Log("[PLAYTEST] state=" + game.State + " wave=" + game.Wave + " hp=" + game.Hp + "/" + game.MaxHp + " attempt=" + game.Attempt + " deaths=" + game.Deaths + " echoes=" + game.EchoMask);
            }

            if (game.Attempt > 6)
            {
                Debug.LogError("[PLAYTEST] too many attempts");
                finished = true;
                Time.timeScale = 1f;
                return;
            }

            switch (game.State)
            {
                case GameState.Title:
                    if (game.StateTime > 0.8f)
                    {
                        Tap("title");
                    }
                    break;
                case GameState.Intro:
                    if (game.StateTime > 0.8f)
                    {
                        Tap("intro");
                    }
                    break;
                case GameState.Calibration:
                    SetYaw(OMTKConfig.CalibrationAzimuth);
                    if (game.StateTime > 0.8f)
                    {
                        Tap("calibration");
                    }
                    break;
                case GameState.Playing:
                    UpdatePlaying();
                    break;
                case GameState.Interlude:
                    if (game.StateTime > 1.0f)
                    {
                        Tap("interlude");
                    }
                    break;
                case GameState.Death:
                    if (game.StateTime > 1.2f)
                    {
                        Tap("retry");
                    }
                    break;
                case GameState.Finale:
                    UpdateFinale();
                    break;
                case GameState.Victory:
                    if (!finished)
                    {
                        finished = true;
                        Time.timeScale = 1f;
                        Debug.Log("[PLAYTEST] VICTORY runSeconds=" + (Time.unscaledTime - runStartedAt).ToString("F1"));
                    }
                    break;
            }
        }

        private void UpdatePlaying()
        {
            OMTKEnemy token = GetToken();
            if (token != lastToken)
            {
                lastToken = token;
                if (token != null)
                {
                    Debug.Log("[PLAYTEST] token=" + token.Variant + " windup=" + token.WindupRemaining.ToString("F2") + " locked=" + token.IsResonanceLocked);
                }
            }

            if (token != null)
            {
                SetYaw(token.Azimuth);
                if (token.IsWindingUp && token.WindupRemaining <= game.PerfectWindow)
                {
                    Tap("perfect@" + token.WindupRemaining.ToString("F3") + " locked=" + token.IsResonanceLocked);
                }

                return;
            }

            OMTKEnemy staggered = FindStaggered();
            if (staggered != null)
            {
                SetYaw(staggered.Azimuth);
                game.SendMessage("HandleSwipe");
                Debug.Log("[PLAYTEST] slash staggered " + staggered.Variant);
            }
        }

        private void UpdateFinale()
        {
            if (playerYawField != null)
            {
                playerYawField.SetValue(game, 180f);
            }

            bool facing = finaleFacingField != null && (bool)finaleFacingField.GetValue(game);
            if (facing && !finaleSwiped)
            {
                finaleSwiped = true;
                game.SendMessage("HandleSwipe");
                Debug.Log("[PLAYTEST] finale swipe");
            }
        }

        private void Tap(string reason)
        {
            game.SendMessage("HandleTap");
            Debug.Log("[PLAYTEST] tap " + reason);
        }

        private void SetYaw(float yaw)
        {
            if (playerYawField != null)
            {
                playerYawField.SetValue(game, Mathf.Repeat(yaw, 360f));
            }
        }

        private OMTKEnemy GetToken()
        {
            return tokenField == null ? null : tokenField.GetValue(game) as OMTKEnemy;
        }

        private OMTKEnemy FindStaggered()
        {
            IList list = enemiesField == null ? null : enemiesField.GetValue(game) as IList;
            if (list == null)
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                OMTKEnemy enemy = list[i] as OMTKEnemy;
                if (enemy != null && enemy.IsStaggered)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
