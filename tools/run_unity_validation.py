"""Run the actual PlayMode suite in an existing Unity 6.3 HDRP project.

Never overwrites the user's project, downloads an editor, or treats missing/skipped
results as a pass. A licensed Unity Editor and real HDRP project are prerequisites.
"""
import argparse,hashlib,json,os,subprocess,sys,time,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]

def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--unity',required=True,type=Path);parser.add_argument('--project',required=True,type=Path);parser.add_argument('--results',required=True,type=Path);parser.add_argument('--filter');parser.add_argument('--headless',action='store_true');parser.add_argument('--timeout',type=int,default=1800);args=parser.parse_args()
 out=args.results.resolve();out.mkdir(parents=True,exist_ok=True);report_path=out/'native-validation.json'
 if report_path.exists():raise SystemExit('Use a new results directory; previous evidence is preserved')
 report={'nativeExecuted':False,'status':'preflight','unity':str(args.unity),'project':str(args.project),'fullSuite':not bool(args.filter),'headless':args.headless,'visualAndFpsCertified':False,'startedUtc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())}
 def finish(status,code,reason=None):
  report['status']=status
  if reason:report['reason']=reason
  report_path.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report));return code
 if not args.unity.is_file():return finish('blocked_unity_editor_missing',2,'Provide the installed Unity 6.3 Editor executable; no native test was run')
 project=args.project.resolve();settings=project/'ProjectSettings/ProjectVersion.txt';manifest=project/'Packages/manifest.json'
 if not settings.is_file() or not manifest.is_file():return finish('blocked_unity_project_missing',2,'This repository ships an Assets package; choose an existing Unity HDRP project')
 if '6000.3.' not in settings.read_text():return finish('blocked_unity_version_mismatch',2,'Expected Unity 6.3 (6000.3.x)')
 dependencies=json.loads(manifest.read_text()).get('dependencies',{});hdrp=dependencies.get('com.unity.render-pipelines.high-definition','')
 if not hdrp.startswith('17.3.'):return finish('blocked_hdrp_version_mismatch',2,'Expected HDRP 17.3.x in the project manifest')
 if 'com.unity.test-framework' not in dependencies:return finish('blocked_test_framework_missing',2,'Install Unity Test Framework using the project Package Manager')
 source=ROOT/'Assets/PoolroomsProcedural';target=project/'Assets/PoolroomsProcedural';missing=[];different=[];matched=0
 for p in source.rglob('*'):
  if not p.is_file() or p.suffix=='.meta':continue
  q=target/p.relative_to(source)
  if not q.is_file():missing.append(p.relative_to(source).as_posix())
  elif hashlib.sha256(q.read_bytes()).digest()!=hashlib.sha256(p.read_bytes()).digest():different.append(p.relative_to(source).as_posix())
  else:matched+=1
 report['packageFilesMatched']=matched
 if missing or different:
  report['missingFiles']=missing;report['differentFiles']=different;return finish('blocked_package_mismatch',2,'Import this revision into the project first; this tool never replaces local work')
 result=out/'playmode.xml';log=out/'unity.log'
 if result.exists() or log.exists():return finish('blocked_results_not_fresh',2,'Choose a new results directory; stale XML cannot validate this run')
 command=[str(args.unity.resolve()),'-batchmode','-projectPath',str(project),'-runTests','-testPlatform','PlayMode','-testResults',str(result),'-logFile',str(log)]
 if args.headless:command.append('-nographics')
 if args.filter:command.extend(['-testFilter',args.filter])
 report['command']=command;report['nativeExecuted']=True
 try:
  run=subprocess.run(command,timeout=args.timeout,check=False);report['editorExitCode']=run.returncode
 except subprocess.TimeoutExpired:return finish('failed_timeout',1,'Editor exceeded the timeout; inspect unity.log')
 except OSError as e:report['nativeExecuted']=False;return finish('blocked_editor_launch',2,str(e))
 if not result.is_file():return finish('failed_no_results',1,'No fresh NUnit XML: inspect compilation, package resolution and licensing in unity.log')
 try:tree=ET.parse(result)
 except ET.ParseError as e:return finish('failed_malformed_results',1,str(e))
 cases=tree.findall('.//test-case');counts={key:sum(c.get('result')==key for c in cases) for key in ['Passed','Failed','Skipped','Inconclusive']};report['results']=counts;report['testCases']=len(cases);report['xmlSha256']=hashlib.sha256(result.read_bytes()).hexdigest()
 if run.returncode or counts['Failed'] or counts['Inconclusive'] or counts['Passed']==0:return finish('failed_native_tests',1,'Read NUnit failures and unity.log; an empty or unsuccessful run is not validation')
 if counts['Skipped']:return finish('passed_executed_tests_with_skips',3,'Executed tests passed, but skipped tests remain unverified')
 return finish('passed_native_suite' if report['fullSuite'] else 'passed_filtered_native_tests',0,'Rendering appearance, ray tracing and FPS require the separate in-game capture and visual inspection')
if __name__=='__main__':sys.exit(main())
