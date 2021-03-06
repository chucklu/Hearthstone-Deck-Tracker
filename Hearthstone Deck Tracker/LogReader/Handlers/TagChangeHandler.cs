#region

using System;
using System.Linq;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Enums.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.Entities;
using Hearthstone_Deck_Tracker.LogReader.Interfaces;
using Hearthstone_Deck_Tracker.Replay;
using static Hearthstone_Deck_Tracker.Enums.GAME_TAG;
using static Hearthstone_Deck_Tracker.Enums.Hearthstone.TAG_PLAYSTATE;
using static Hearthstone_Deck_Tracker.Enums.Hearthstone.TAG_ZONE;
using static Hearthstone_Deck_Tracker.Replay.KeyPointType;

#endregion

namespace Hearthstone_Deck_Tracker.LogReader.Handlers
{
	class TagChangeHandler
	{
		public void TagChange(IHsGameState gameState, string rawTag, int id, string rawValue, IGame game, bool isRecursive = false)
		{
			if(gameState.LastId != id)
			{
				//game.SecondToLastUsedId = gameState.LastId;
				if(gameState.ProposedKeyPoint != null)
				{
					ReplayMaker.Generate(gameState.ProposedKeyPoint.Type, gameState.ProposedKeyPoint.Id, gameState.ProposedKeyPoint.Player, game);
					gameState.ProposedKeyPoint = null;
				}
			}
			gameState.LastId = id;
			if(id > gameState.MaxId)
				gameState.MaxId = id;
			if(!game.Entities.ContainsKey(id))
				game.Entities.Add(id, new Entity(id));
			GAME_TAG tag;
			if(!Enum.TryParse(rawTag, out tag))
			{
				int tmp;
				if(int.TryParse(rawTag, out tmp) && Enum.IsDefined(typeof(GAME_TAG), tmp))
					tag = (GAME_TAG)tmp;
			}
			var value = LogReaderHelper.ParseTagValue(tag, rawValue);
			var prevValue = game.Entities[id].GetTag(tag);
			game.Entities[id].SetTag(tag, value);


			if(tag == CONTROLLER && gameState.WaitForController != null && game.Player.Id == -1)
			{
				var p1 = game.Entities.FirstOrDefault(e => e.Value.GetTag(PLAYER_ID) == 1).Value;
				var p2 = game.Entities.FirstOrDefault(e => e.Value.GetTag(PLAYER_ID) == 2).Value;
				if(gameState.CurrentEntityHasCardId)
				{
					if(p1 != null)
						p1.IsPlayer = value == 1;
					if(p2 != null)
						p2.IsPlayer = value != 1;
					game.Player.Id = value;
					game.Opponent.Id = value % 2 + 1;
				}
				else
				{
					if(p1 != null)
						p1.IsPlayer = value != 1;
					if(p2 != null)
						p2.IsPlayer = value == 1;
					game.Player.Id = value % 2 + 1;
					game.Opponent.Id = value;
				}
			}
			var controller = game.Entities[id].GetTag(CONTROLLER);
			var cardId = game.Entities[id].CardId;
			if(tag == ZONE)
			{
				if(((TAG_ZONE)value == HAND
				    || ((TAG_ZONE)value == PLAY || (TAG_ZONE)value == DECK) && game.IsMulliganDone)
				   && gameState.WaitForController == null)
				{
					if(!game.IsMulliganDone)
						prevValue = (int)DECK;
					if(controller == 0)
					{
						game.Entities[id].SetTag(ZONE, prevValue);
						gameState.WaitForController = new {Tag = rawTag, Id = id, Value = rawValue};
						return;
					}
				}
				switch((TAG_ZONE)prevValue)
				{
					case DECK:
						switch((TAG_ZONE)value)
						{
							case HAND:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerDraw(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(Draw, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									if(!string.IsNullOrEmpty(game.Entities[id].CardId))
									{
#if DEBUG
										Logger.WriteLine($"Opponent Draw (EntityID={id}) already has a CardID. Removing. Blizzard Pls.", "TagChange");
#endif
										game.Entities[id].CardId = string.Empty;
									}
									gameState.GameHandler.HandleOpponentDraw(game.Entities[id], gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(Draw, id, ActivePlayer.Opponent);
								}
								break;
							case SETASIDE:
							case REMOVEDFROMGAME:
								if(controller == game.Player.Id)
								{
									if(gameState.JoustReveals > 0)
									{
										gameState.JoustReveals--;
										break;
									}
									gameState.GameHandler.HandlePlayerRemoveFromDeck(game.Entities[id], gameState.GetTurnNumber());
								}
								else if(controller == game.Opponent.Id)
								{
									if(gameState.JoustReveals > 0)
									{
										gameState.JoustReveals--;
										break;
									}
									gameState.GameHandler.HandleOpponentRemoveFromDeck(game.Entities[id], gameState.GetTurnNumber());
								}
								break;
							case GRAVEYARD:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerDeckDiscard(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(DeckDiscard, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentDeckDiscard(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(DeckDiscard, id, ActivePlayer.Opponent);
								}
								break;
							case PLAY:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerDeckToPlay(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(DeckDiscard, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentDeckToPlay(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(DeckDiscard, id, ActivePlayer.Opponent);
								}
								break;
							case TAG_ZONE.SECRET:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerSecretPlayed(game.Entities[id], cardId, gameState.GetTurnNumber(), true);
									gameState.ProposeKeyPoint(SecretPlayed, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentSecretPlayed(game.Entities[id], cardId, -1, gameState.GetTurnNumber(), true, id);
									gameState.ProposeKeyPoint(SecretPlayed, id, ActivePlayer.Player);
								}
								break;
							default:
								Logger.WriteLine($"WARNING - unhandled zone change (id={id}): {prevValue} -> {value}", "TagChange");
								break;
						}
						break;
					case HAND:
						switch((TAG_ZONE)value)
						{
							case PLAY:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerPlay(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(Play, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentPlay(game.Entities[id], cardId, game.Entities[id].GetTag(ZONE_POSITION),
									                                         gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(Play, id, ActivePlayer.Opponent);
								}
								break;
							case REMOVEDFROMGAME:
							case SETASIDE:
							case GRAVEYARD:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerHandDiscard(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(HandDiscard, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentHandDiscard(game.Entities[id], cardId, game.Entities[id].GetTag(ZONE_POSITION),
									                                                gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(HandDiscard, id, ActivePlayer.Opponent);
								}
								break;
							case TAG_ZONE.SECRET:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerSecretPlayed(game.Entities[id], cardId, gameState.GetTurnNumber(), false);
									gameState.ProposeKeyPoint(SecretPlayed, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentSecretPlayed(game.Entities[id], cardId, game.Entities[id].GetTag(ZONE_POSITION),
									                                                 gameState.GetTurnNumber(), false, id);
									gameState.ProposeKeyPoint(SecretPlayed, id, ActivePlayer.Opponent);
								}
								break;
							case DECK:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerMulligan(game.Entities[id], cardId);
									gameState.ProposeKeyPoint(Mulligan, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentMulligan(game.Entities[id], game.Entities[id].GetTag(ZONE_POSITION));
									gameState.ProposeKeyPoint(Mulligan, id, ActivePlayer.Opponent);
								}
								break;
							default:
								Logger.WriteLine($"WARNING - unhandled zone change (id={id}): {prevValue} -> {value}", "TagChange");
								break;
						}
						break;
					case PLAY:
						switch((TAG_ZONE)value)
						{
							case HAND:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerBackToHand(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(PlayToHand, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentPlayToHand(game.Entities[id], cardId, gameState.GetTurnNumber(), id);
									gameState.ProposeKeyPoint(PlayToHand, id, ActivePlayer.Opponent);
								}
								break;
							case DECK:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerPlayToDeck(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(PlayToDeck, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentPlayToDeck(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(PlayToDeck, id, ActivePlayer.Opponent);
								}
								break;
							case REMOVEDFROMGAME:
							case SETASIDE:
							case GRAVEYARD:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerPlayToGraveyard(game.Entities[id], cardId, gameState.GetTurnNumber());
									if(game.Entities[id].HasTag(HEALTH))
										gameState.ProposeKeyPoint(Death, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentPlayToGraveyard(game.Entities[id], cardId, gameState.GetTurnNumber(),
									                                                    gameState.PlayersTurn());
									if(game.Entities[id].HasTag(HEALTH))
										gameState.ProposeKeyPoint(Death, id, ActivePlayer.Opponent);
								}
								break;
							default:
								Logger.WriteLine($"WARNING - unhandled zone change (id={id}): {prevValue} -> {value}", "TagChange");
								break;
						}
						break;
					case TAG_ZONE.SECRET:
						switch((TAG_ZONE)value)
						{
							case TAG_ZONE.SECRET:
							case GRAVEYARD:
								if(controller == game.Player.Id)
									gameState.ProposeKeyPoint(SecretTriggered, id, ActivePlayer.Player);
								if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentSecretTrigger(game.Entities[id], cardId, gameState.GetTurnNumber(), id);
									gameState.ProposeKeyPoint(SecretTriggered, id, ActivePlayer.Opponent);
								}
								break;
							default:
								Logger.WriteLine($"WARNING - unhandled zone change (id={id}): {prevValue} -> {value}", "TagChange");
								break;
						}
						break;
					case GRAVEYARD:
					case SETASIDE:
					case CREATED:
					case TAG_ZONE.INVALID:
					case REMOVEDFROMGAME:
						switch((TAG_ZONE)value)
						{
							case PLAY:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerCreateInPlay(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(Summon, id, ActivePlayer.Player);
								}
								if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentCreateInPlay(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(Summon, id, ActivePlayer.Opponent);
								}
								break;
							case DECK:
								if(controller == game.Player.Id)
								{
									if(gameState.JoustReveals > 0)
										break;
									gameState.GameHandler.HandlePlayerGetToDeck(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(CreateToDeck, id, ActivePlayer.Player);
								}
								if(controller == game.Opponent.Id)
								{
									if(gameState.JoustReveals > 0)
										break;
									gameState.GameHandler.HandleOpponentGetToDeck(game.Entities[id], gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(CreateToDeck, id, ActivePlayer.Opponent);
								}
								break;
							case HAND:
								if(controller == game.Player.Id)
								{
									gameState.GameHandler.HandlePlayerGet(game.Entities[id], cardId, gameState.GetTurnNumber());
									gameState.ProposeKeyPoint(Obtain, id, ActivePlayer.Player);
								}
								else if(controller == game.Opponent.Id)
								{
									gameState.GameHandler.HandleOpponentGet(game.Entities[id], gameState.GetTurnNumber(), id);
									gameState.ProposeKeyPoint(Obtain, id, ActivePlayer.Opponent);
								}
								break;
							default:
								Logger.WriteLine($"WARNING - unhandled zone change (id={id}): {prevValue} -> {value}", "TagChange");
								break;
						}
						break;
					default:
						Logger.WriteLine($"WARNING - unhandled zone change (id={id}): {prevValue} -> {value}", "TagChange");
						break;
				}
			}
			else if(tag == PLAYSTATE)
			{
				if(value == (int)CONCEDED)
					gameState.GameHandler.HandleConcede();
				if(!gameState.GameEnded)
				{
					if(game.Entities[id].IsPlayer)
					{
						switch((TAG_PLAYSTATE)value)
						{
							case WON:
								gameState.GameEndKeyPoint(true, id);
								gameState.GameHandler.HandleWin();
								gameState.GameHandler.HandleGameEnd();
								gameState.GameEnded = true;
								break;
							case LOST:
								gameState.GameEndKeyPoint(false, id);
								gameState.GameHandler.HandleLoss();
								gameState.GameHandler.HandleGameEnd();
								gameState.GameEnded = true;
								break;
							case TIED:
								gameState.GameEndKeyPoint(false, id);
								gameState.GameHandler.HandleTied();
								gameState.GameHandler.HandleGameEnd();
								break;
						}
					}
				}
			}
			else if(tag == CARDTYPE && value == (int)TAG_CARDTYPE.HERO)
				SetHeroAsync(id, game, gameState);
			else if(tag == CURRENT_PLAYER && value == 1)
			{
				var activePlayer = game.Entities[id].IsPlayer ? ActivePlayer.Player : ActivePlayer.Opponent;
				gameState.GameHandler.TurnStart(activePlayer, gameState.GetTurnNumber());
				if(activePlayer == ActivePlayer.Player)
					gameState.PlayerUsedHeroPower = false;
				else
					gameState.OpponentUsedHeroPower = false;
			}
			else if(tag == LAST_CARD_PLAYED)
				gameState.LastCardPlayed = value;
			else if(tag == DEFENDING)
			{
				if(controller == game.Opponent.Id)
					gameState.GameHandler.HandleDefendingEntity(value == 1 ? game.Entities[id] : null);
			}
			else if(tag == ATTACKING)
			{
				if(controller == game.Player.Id)
					gameState.GameHandler.HandleAttackingEntity(value == 1 ? game.Entities[id] : null);
			}
			else if(tag == PROPOSED_DEFENDER)
				game.OpponentSecrets.ProposedDefenderEntityId = value;
			else if(tag == PROPOSED_ATTACKER)
				game.OpponentSecrets.ProposedAttackerEntityId = value;
			else if(tag == NUM_MINIONS_PLAYED_THIS_TURN && value > 0)
			{
				if(gameState.PlayersTurn())
					gameState.GameHandler.HandlePlayerMinionPlayed();
			}
			else if(tag == PREDAMAGE && value > 0)
			{
				if(gameState.PlayersTurn())
					gameState.GameHandler.HandleOpponentDamage(game.Entities[id]);
			}
			else if(tag == NUM_TURNS_IN_PLAY && value > 0)
			{
				if(!gameState.PlayersTurn())
					gameState.GameHandler.HandleOpponentTurnStart(game.Entities[id]);
			}
			else if(tag == NUM_ATTACKS_THIS_TURN && value > 0)
			{
				if(controller == game.Player.Id)
					gameState.ProposeKeyPoint(Attack, id, ActivePlayer.Player);
				else if(controller == game.Opponent.Id)
					gameState.ProposeKeyPoint(Attack, id, ActivePlayer.Opponent);
			}
			else if(tag == ZONE_POSITION)
			{
				var entity = game.Entities[id];
				var zone = entity.GetTag(ZONE);
				if(zone == (int)HAND)
				{
					if(controller == game.Player.Id)
					{
						ReplayMaker.Generate(HandPos, id, ActivePlayer.Player, game);
						gameState.GameHandler.HandleZonePositionUpdate(ActivePlayer.Player, entity, HAND, gameState.GetTurnNumber());
					}
					else if(controller == game.Opponent.Id)
					{
						ReplayMaker.Generate(HandPos, id, ActivePlayer.Opponent, game);
						gameState.GameHandler.HandleZonePositionUpdate(ActivePlayer.Opponent, entity, HAND, gameState.GetTurnNumber());
					}
				}
				else if(zone == (int)PLAY)
				{
					if(controller == game.Player.Id)
					{
						ReplayMaker.Generate(BoardPos, id, ActivePlayer.Player, game);
						gameState.GameHandler.HandleZonePositionUpdate(ActivePlayer.Player, entity, PLAY, gameState.GetTurnNumber());
					}
					else if(controller == game.Opponent.Id)
					{
						ReplayMaker.Generate(BoardPos, id, ActivePlayer.Opponent, game);
						gameState.GameHandler.HandleZonePositionUpdate(ActivePlayer.Opponent, entity, PLAY, gameState.GetTurnNumber());
					}
				}
			}
			else if(tag == CARD_TARGET && value > 0)
			{
				if(controller == game.Player.Id)
					gameState.ProposeKeyPoint(PlaySpell, id, ActivePlayer.Player);
				else if(controller == game.Opponent.Id)
					gameState.ProposeKeyPoint(PlaySpell, id, ActivePlayer.Opponent);
			}
			else if(tag == EQUIPPED_WEAPON && value == 0)
			{
				if(controller == game.Player.Id)
					gameState.ProposeKeyPoint(WeaponDestroyed, id, ActivePlayer.Player);
				else if(controller == game.Opponent.Id)
					gameState.ProposeKeyPoint(WeaponDestroyed, id, ActivePlayer.Opponent);
			}
			else if(tag == EXHAUSTED && value > 0)
			{
				if(game.Entities[id].GetTag(CARDTYPE) == (int)TAG_CARDTYPE.HERO_POWER)
				{
					if(controller == game.Player.Id)
						gameState.ProposeKeyPoint(HeroPower, id, ActivePlayer.Player);
					else if(controller == game.Opponent.Id)
						gameState.ProposeKeyPoint(HeroPower, id, ActivePlayer.Opponent);
				}
			}
			else if(tag == CONTROLLER && prevValue > 0)
			{
				if(value == game.Player.Id)
				{
					if(game.Entities[id].IsInZone(TAG_ZONE.SECRET))
					{
						gameState.GameHandler.HandleOpponentStolen(game.Entities[id], cardId, gameState.GetTurnNumber());
						gameState.ProposeKeyPoint(SecretStolen, id, ActivePlayer.Player);
					}
					else if(game.Entities[id].IsInZone(PLAY))
						gameState.GameHandler.HandleOpponentStolen(game.Entities[id], cardId, gameState.GetTurnNumber());
				}
				else if(value == game.Opponent.Id)
				{
					if(game.Entities[id].IsInZone(TAG_ZONE.SECRET))
					{
						gameState.GameHandler.HandleOpponentStolen(game.Entities[id], cardId, gameState.GetTurnNumber());
						gameState.ProposeKeyPoint(SecretStolen, id, ActivePlayer.Player);
					}
					else if(game.Entities[id].IsInZone(PLAY))
						gameState.GameHandler.HandlePlayerStolen(game.Entities[id], cardId, gameState.GetTurnNumber());
				}
			}
			else if(tag == FATIGUE)
			{
				if(controller == game.Player.Id)
					gameState.GameHandler.HandlePlayerFatigue(Convert.ToInt32(rawValue));
				else if(controller == game.Opponent.Id)
					gameState.GameHandler.HandleOpponentFatigue(Convert.ToInt32(rawValue));
			}
			if(gameState.WaitForController != null)
			{
				if(!isRecursive)
				{
					TagChange(gameState, (string)gameState.WaitForController.Tag, (int)gameState.WaitForController.Id,
					          (string)gameState.WaitForController.Value, game, true);
					gameState.WaitForController = null;
				}
			}
		}

		private async void SetHeroAsync(int id, IGame game, IHsGameState gameState)
		{
			Logger.WriteLine("Found hero with id=" + id, "TagChangeHandler");
			if(game.PlayerEntity == null)
			{
				Logger.WriteLine("Waiting for PlayerEntity to exist", "TagChangeHandler");
				while(game.PlayerEntity == null)
					await Task.Delay(100);
				Logger.WriteLine("Found PlayerEntity", "TagChangeHandler");
			}
			if(string.IsNullOrEmpty(game.Player.Class) && id == game.PlayerEntity.GetTag(HERO_ENTITY))
			{
				gameState.GameHandler.SetPlayerHero(Database.GetHeroNameFromId(game.Entities[id].CardId));
				return;
			}
			if(game.OpponentEntity == null)
			{
				Logger.WriteLine("Waiting for OpponentEntity to exist", "TagChangeHandler");
				while(game.OpponentEntity == null)
					await Task.Delay(100);
				Logger.WriteLine("Found OpponentEntity", "TagChangeHandler");
			}
			if(string.IsNullOrEmpty(game.Opponent.Class) && id == game.OpponentEntity.GetTag(HERO_ENTITY))
				gameState.GameHandler.SetOpponentHero(Database.GetHeroNameFromId(game.Entities[id].CardId));
		}
	}
}