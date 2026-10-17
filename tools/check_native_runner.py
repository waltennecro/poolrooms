"""Exercise validation-result handling with a clearly fake editor process.
These are runner contract tests, NOT Unity compilation or PlayMode results.
"""
from pathlib import Path
import json,subprocess,sys,tempfile,os
ROOT=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory() as temp:
 root=Path(temp);project=root/'fixture-project';(project/'ProjectSettings').mkdir(parents=True);(project/'Packages').mkdir()
 (project/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.17f1\n');(project/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.render-pipelines.high-definition':'17.3.0','com.unity.test-framework':'1.6.0'}}))
 (project/'Assets').mkdir();(project/'Assets/PoolroomsProcedural').symlink_to(ROOT/'Assets/PoolroomsProcedural',target_is_directory=True)
 editor=root/'FAKE-editor-for-contract-test'
 editor.write_text('''#!/usr/bin/env python3
import sys,pathlib
out=pathlib.Path(sys.argv[sys.argv.index('-testResults')+1])
mode=out.parent.name
xml='<test-run><test-case result="Passed" /></test-run>'
if mode=='zero':xml='<test-run />'
if mode=='skipped':xml='<test-run><test-case result="Skipped" /></test-run>'
if mode=='mixed':xml='<test-run><test-case result="Passed" /><test-case result="Skipped" /></test-run>'
if mode=='failed':xml='<test-run><test-case result="Failed" /></test-run>'
if mode!='absent':out.write_text(xml)
''');editor.chmod(0o755)
 for name,expected in [('passed',0),('zero',1),('skipped',1),('mixed',3),('failed',1),('absent',1)]:
  out=root/name;run=subprocess.run([sys.executable,str(ROOT/'tools/run_unity_validation.py'),'--unity',str(editor),'--project',str(project),'--results',str(out)],capture_output=True,text=True)
  assert run.returncode==expected,(name,run.stdout,run.stderr)
  report=json.loads((out/'native-validation.json').read_text());assert not report['visualAndFpsCertified']
 out=root/'stale';out.mkdir();(out/'playmode.xml').write_text('<test-run><test-case result="Passed" /></test-run>')
 run=subprocess.run([sys.executable,str(ROOT/'tools/run_unity_validation.py'),'--unity',str(editor),'--project',str(project),'--results',str(out)],capture_output=True,text=True);assert run.returncode==2
 assert json.loads((out/'native-validation.json').read_text())['status']=='blocked_results_not_fresh'
print('Runner contract checks passed: zero, skipped-only, absent, failed and stale XML cannot report a successful native suite. These fake subprocess checks do NOT execute Unity.')
