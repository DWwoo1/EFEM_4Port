using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Configuration;
using System.Linq.Expressions;
using System.Threading;

using FrameOfSystem3.SECSGEM.DefineSecsGem;
using FrameOfSystem3.Functional;

using Define.DefineEnumProject.Mail;
using Define.DefineEnumProject.Task.Global;

namespace FrameOfSystem3.Work
{
	public class ScenarioCirculator
	{
		private ScenarioCirculator() { }
		private static ScenarioCirculator _instance = null;
		public static ScenarioCirculator Instance
		{
			get
			{
				if (_instance == null)
					_instance = new ScenarioCirculator();

				return _instance;
			}
		}

		public void Initialize(
			Func<Enum, Dictionary<string, string>, bool> funcUpdateParameter,
			Func<Enum, bool> funcExecuteScenraio,
			Action<int, string> funcGenerateAlarm,
			int fdcInterval)
		{
			_updateParameter = funcUpdateParameter;
			_executeScenario = funcExecuteScenraio;
			_generateAlarm = funcGenerateAlarm;

			_postOffice = PostOffice.GetInstance();
			_postOffice.RequestSubscribe(EN_SUBSCRIBER.ScenarioCirculator, (title) => _queueReceiveMail.Enqueue(title));
			_fdcInterval = (uint)fdcInterval;

			Run = false;
			UseOption = false;
		}

		Queue<ScenarioData> _queueScenarioList = new Queue<ScenarioData>();
		Func<Enum, Dictionary<string, string>, bool> _updateParameter = null;
		Func<Enum, bool> _executeScenario = null;
		Action<int, string> _generateAlarm = null;
		PostOffice _postOffice = null;
		ConcurrentQueue<EN_MAIL> _queueReceiveMail = new ConcurrentQueue<EN_MAIL>();
		uint _fdcInterval;

		public bool Run { get; set; }
		public bool UseOption { get; set; }
		public bool IsPossible { get { return Run && UseOption; } }

		public void Execute()
		{
			MailBoxCheck();

			if (false == Run)
				return;

			Circulation();
		}
		public void SetFdcInterval(int fdcInterval)
		{
			_fdcInterval = (uint)fdcInterval;
		}

		private void Circulation()
		{
			if (_queueScenarioList.Count < 1)
				return;

			ScenarioData scenarioData = _queueScenarioList.Peek();
			if (false == scenarioData.UpdatedData)
			{
				if (false == _updateParameter(scenarioData.Scenario, scenarioData.Datas))
				{
					// alarm 띄우니까 dequeue하고 종료
					_queueScenarioList.Dequeue();

					_generateAlarm((int)ALARM_GLOBAL.WrongScenarioCirculation
						, string.Format("parameter update\nscenario : {0}"
						, scenarioData.Scenario.ToString()));
					return;
				}	

				scenarioData.UpdatedData = true;
				return;
			}

			if (false == scenarioData.Finished)
			{
				scenarioData.Finished = _executeScenario(scenarioData.Scenario);
				return;
			}
			else
			{
				_queueScenarioList.Dequeue();
				return;
			}
		}
		private void MailBoxCheck()
		{
			while (_queueReceiveMail.Count > 0)
			{
				EN_MAIL receivedMail;
				_queueReceiveMail.TryDequeue(out receivedMail);

				List<Mail> mailList;
				switch (receivedMail)
				{
					case EN_MAIL.SendScenario:
						#region
						if (false == _postOffice.GetMail(EN_SUBSCRIBER.ScenarioCirculator, receivedMail, out mailList) || mailList == null)
						{
							Console.WriteLine("ScenarioCirculator: GetMail failed");
							return;
						}

						foreach (var mail in mailList)
						{
							if (mail.Content.Count != 2) return;
							var scenario = (Enum)mail.Content[0];
							var datas = (Dictionary<string, string>)mail.Content[1];

							if (false == IsPossible)
								return;

							_queueScenarioList.Enqueue(new ScenarioData(scenario, datas));
						}
						break;
						#endregion
					case EN_MAIL.ScenarioCurculatorRun:
						#region
						{
							if (false == _postOffice.GetMail(EN_SUBSCRIBER.ScenarioCirculator, receivedMail, out mailList) || mailList == null)
							{
								Console.WriteLine("ScenarioCirculator: GetMail failed");
								return;
							}

							bool b = true;
							foreach (var mail in mailList)
							{
								b &= (bool)mail.Content[0];
							}
							Run = b;

							var recipe = Recipe.Recipe.GetInstance();
							if (b)
							{
								UseOption = recipe.GetValue(Recipe.EN_RECIPE_TYPE.COMMON, Recipe.PARAM_COMMON.UseSecsGem.ToString(), false);
							}
							else
							{
								recipe.SetValue(Recipe.EN_RECIPE_TYPE.COMMON, Recipe.PARAM_COMMON.UseSecsGem.ToString(), false.ToString());
							}
							break;
						}
						#endregion
					case EN_MAIL.ScenarioCurculatorUse:
						#region
						{
							if (false == _postOffice.GetMail(EN_SUBSCRIBER.ScenarioCirculator, receivedMail, out mailList) || mailList == null)
							{
								Console.WriteLine("ScenarioCirculator: GetMail failed");
								return;
							}

							bool b = true;
							foreach (var mail in mailList)
							{
								b &= (bool)mail.Content[0];
							}
							UseOption = b;
							SECSGEM.ScenarioOperator.Instance.SetUse(b);
							break;
						}
						#endregion
					//case EN_MAIL.RecipeParameterChanged:
					//	#region 
					//	if (false == _postOffice.GetMail(EN_SUBSCRIBER.ScenarioCirculator, receivedMail, out mailList) || mailList == null)
					//	{
					//		Console.WriteLine("ScenarioCirculator: GetMail failed");
					//		return;
					//	}
					//	else
					//	{
					//		var ppDatas = new Dictionary<string, string>();
					//		foreach (var mail in mailList)
					//		{
					//			if (mail.Content.Count != 1) return;
					//			var itemList = (List<Component.ControlInterface.ParameterItem>)mail.Content[0];
					//			foreach (var item in itemList)
					//			{
					//				if (item.Type.Equals(Recipe.EN_RECIPE_TYPE.PROCESS))
					//				{
					//					string key = string.Format("{0}.{1}", item.TaskName, item.ParameterName);
					//					if (ppDatas.ContainsKey(key))
					//						ppDatas.Remove(key);

					//					ppDatas.Add(key, item.Value);
					//				}
					//			}
					//		}

					//		if (ppDatas.Count > 0)
					//		{
					//			_postOffice.SendMail(EN_SUBSCRIBER.ScenarioCirculator, EN_MAIL.SendScenario
					//				, BaseScenario.ParameterIsChanged, ppDatas);
					//		}
					//	}
					//	break;
					//	#endregion
					default: return;
				}
			}
		}

        private void ReceivedParameterChanged(bool result, List<Recipe.Recipe.ParameterItem> changedList)
        {
            if (false == result)
                return;

            var ppDatas = new Dictionary<string, string>();
            foreach (var item in changedList)
            {
                if (item.Type.Equals(Recipe.EN_RECIPE_TYPE.PROCESS))
                {
                    string key = string.Format("{0}.{1}", item.TaskName, item.ParameterName);
                    if (ppDatas.ContainsKey(key))
                        ppDatas.Remove(key);

                    ppDatas.Add(key, item.Value);
                }
            }

            if (ppDatas.Count > 0)
            {
                _postOffice.SendMail(EN_SUBSCRIBER.ScenarioCirculator, EN_MAIL.SendScenario
                    , "RecipeParameterChanged", ppDatas);
            }
        }


        class ScenarioData
		{
			public ScenarioData(Enum scenario, Dictionary<string, string> datas)
			{
				Scenario = scenario;
				Datas = datas;
				UpdatedData = false;
				Finished = false;
			}

			public readonly Enum Scenario;
			public readonly Dictionary<string, string> Datas;
			public bool UpdatedData { get; set; }
			public bool Finished { get; set; }
		}
	}

	public class RecipeSaveLock
	{
		#region singleton
		private RecipeSaveLock() { }
		private static RecipeSaveLock _instance = null;
		public static RecipeSaveLock Instance
		{
			get
			{
				if (_instance == null)
					_instance = new RecipeSaveLock();

				return _instance;
			}
		}
		#endregion /singleton

		bool _use = true;
		bool _isLock = false;
		bool _enable = true;
		ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
		ReaderWriterLockSlim _rwUse = new ReaderWriterLockSlim();
		ReaderWriterLockSlim _rwEnable = new ReaderWriterLockSlim();

		public void SetUse(bool trigger)
		{
			if (false == IsEnable)
				trigger = false;

			_rwUse.EnterWriteLock();
			_use = trigger;
			_rwUse.ExitWriteLock();
		}
		private bool IsUse
		{
			get
			{
				if (false == IsEnable)
					return false;

				_rwUse.EnterReadLock();
				bool r = _use;
				_rwUse.ExitReadLock();
				return r;

			}
		}
		public void Lock()
		{
			if (IsUse)
			{
				_rwLock.EnterWriteLock();
				_isLock = true;
				_rwLock.ExitWriteLock();
			}
		}
		public void Unlock()
		{
			_rwLock.EnterWriteLock();
			_isLock = false;
			_rwLock.ExitWriteLock();
		}
		public bool IsLock
		{
			get
			{
				if (false == IsUse)
					return false;

				_rwLock.EnterReadLock();
				bool r = _isLock;
				_rwLock.ExitReadLock();

				return r;
			}
		}
		public void SetEnable(bool trigger)
		{
			_rwEnable.EnterWriteLock();
			_enable = trigger;
			_rwEnable.ExitWriteLock();
		}
		private bool IsEnable
		{
			get
			{
				_rwEnable.EnterReadLock();
				bool r = _enable;
				_rwEnable.ExitReadLock();
				return r;
			}
		}
	}
}
