using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.UI;

namespace TcgEngine.FX
{
    /// <summary>
    /// 与任何卡牌或玩家无关的特效（FX），
    /// 通常显示在游戏板中央，用于大型技能或全局效果
    /// </summary>
    public class GameBoardFX : MonoBehaviour
    {
        void Start()
        {
            // 获取客户端实例，订阅全局事件
            GameClient client = GameClient.Get();
            client.onNewTurn += OnNewTurn;         // 新回合开始
            client.onCardPlayed += OnPlayCard;     // 卡牌被打出
            client.onAbilityStart += OnAbility;    // 技能开始
            client.onSecretTrigger += OnSecret;    // 秘密卡被触发
            client.onValueRolled += OnRoll;        // 骰子/随机值掷出
        }

        /// <summary>
        /// 新回合开始时播放特效和音效
        /// </summary>
        void OnNewTurn(int player_id)
        {
            AudioTool.Get().PlaySFX("turn", AssetData.Get().new_turn_audio);  // 播放新回合音效
            FXTool.DoFX(AssetData.Get().new_turn_fx, Vector3.zero);           // 播放新回合特效（中心位置）
        }

        /// <summary>
        /// 当卡牌被打出时触发特效和音效
        /// </summary>
        void OnPlayCard(Card card, Slot slot)
        {
            int player_id = GameClient.Get().GetPlayerID();  // 当前玩家ID
            if (card != null)
            {
                CardData icard = CardData.Get(card.card_id);

                // 如果是法术卡
                if (icard.type == CardType.Spell)
                {
                    // 区分自己打出还是对手打出
                    GameObject prefab = player_id == card.player_id ? AssetData.Get().play_card_fx : AssetData.Get().play_card_other_fx;
                    GameObject obj = FXTool.DoFX(prefab, Vector3.zero);  // 播放FX
                    CardUI ui = obj.GetComponentInChildren<CardUI>();
                    ui.SetCard(icard, card.VariantData);                // 设置卡牌UI显示

                    AudioClip spawn_audio = icard.spawn_audio != null ? icard.spawn_audio : AssetData.Get().card_spawn_audio;
                    AudioTool.Get().PlaySFX("card_spell", spawn_audio); // 播放音效
                }

                // 如果是秘密卡
                if (icard.type == CardType.Secret)
                {
                    GameObject sprefab = player_id == card.player_id ? AssetData.Get().play_secret_fx : AssetData.Get().play_secret_other_fx;
                    FXTool.DoFX(sprefab, Vector3.zero);                  // 播放FX

                    AudioClip spawn_audio = icard.spawn_audio != null ? icard.spawn_audio : AssetData.Get().card_spawn_audio;
                    AudioTool.Get().PlaySFX("card_spell", spawn_audio); // 播放音效
                }
            }
        }

        /// <summary>
        /// 技能开始时播放全局特效
        /// </summary>
        private void OnAbility(AbilityData iability, Card caster)
        {
            if (iability != null)
            {
                FXTool.DoFX(iability.board_fx, Vector3.zero);  // 播放技能FX（中心位置）
            }
        }

        /// <summary>
        /// 秘密卡被触发时播放音效
        /// </summary>
        private void OnSecret(Card secret, Card triggerer)
        {
            CardData icard = CardData.Get(secret.card_id);
            if (icard?.attack_audio != null)
                AudioTool.Get().PlaySFX("card_secret", icard.attack_audio);
        }

        /// <summary>
        /// 掷骰子/随机值事件时播放骰子FX
        /// </summary>
        private void OnRoll(int value)
        {
            GameObject fx = FXTool.DoFX(AssetData.Get().dice_roll_fx, Vector3.zero); // 播放骰子FX
            DiceRollFX dice = fx?.GetComponent<DiceRollFX>();
            if (dice != null)
            {
                dice.value = value; // 设置骰子点数
            }
        }
    }
}
