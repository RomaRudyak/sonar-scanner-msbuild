﻿//-----------------------------------------------------------------------
// <copyright file="PropertiesWriter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarQube.Common;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SonarRunner.Shim
{
    public static class PropertiesWriter
    {
        public static string ToString(ILogger logger, string projectKey, string projectName, string projectVersion, string projectBaseDir, List<Project> projects)
        {
            var uniqueProjects = projects.GroupBy(p => p.GuidAsString()).Where(g => g.Count() == 1).Select(g => g.First());
            foreach (var duplicatedProject in projects.Where(p => !uniqueProjects.Any(p2 => p.GuidAsString().Equals(p2.GuidAsString())))) {
                logger.LogWarning(Resources.WARN_DuplicateProjectGuid, duplicatedProject.GuidAsString(), duplicatedProject.MsBuildProject);
            }

            StringBuilder sb = new StringBuilder();

            AppendKeyValue(sb, "sonar.projectKey", projectKey);
            AppendKeyValue(sb, "sonar.projectName", projectName);
            AppendKeyValue(sb, "sonar.projectVersion", projectVersion);
            AppendKeyValue(sb, "sonar.projectBaseDir", projectBaseDir);
            sb.AppendLine();

            sb.AppendLine("# FIXME: Encoding is hardcoded");
            AppendKeyValue(sb, "sonar.sourceEncoding", "UTF-8");
            sb.AppendLine();

            AppendKeyValue(sb, "sonar.modules", string.Join(",", uniqueProjects.Select(p => p.GuidAsString())));
            sb.AppendLine();

            foreach (var project in uniqueProjects)
            {
                string guid = project.GuidAsString();
                
                AppendKeyValue(sb, guid, "sonar.projectKey", projectKey + ":" + guid);
                AppendKeyValue(sb, guid, "sonar.projectName", project.Name);
                AppendKeyValue(sb, guid, "sonar.projectBaseDir", project.BaseDir());
                if (project.FxCopReport != null)
                {
                    AppendKeyValue(sb, guid, "sonar.cs.fxcop.reportPath", project.FxCopReport);
                }
                if (project.VisualStudioCodeCoverageReport != null)
                {
                    AppendKeyValue(sb, guid, "sonar.cs.vscoveragexml.reportsPaths", project.VisualStudioCodeCoverageReport);
                }
                if (!project.IsTest)
                {
                    sb.AppendLine(guid + @".sonar.sources=\");
                }
                else
                {
                    AppendKeyValue(sb, guid, "sonar.sources", "");
                    sb.AppendLine(guid + @".sonar.tests=\");
                }
                var files = project.FilesToAnalyze();
                for (int i = 0; i < files.Count(); i++)
                {
                    var file = files[i];
                    sb.Append(Escape(file));
                    if (i != files.Count() - 1)
                    {
                        sb.Append(@",\");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void AppendKeyValue(StringBuilder sb, string keyPrefix, string keySuffix, string value)
        {
            AppendKeyValue(sb, keyPrefix + "." + keySuffix, value);
        }

        private static void AppendKeyValue(StringBuilder sb, string key, string value)
        {
            sb.Append(key);
            sb.Append('=');
            sb.AppendLine(Escape(value));
        }

        public static string Escape(string value)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in value)
            {
                if (c == '\\')
                {
                    sb.Append("\\\\");
                }
                else if (IsAscii(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append("\\u");
                    sb.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                }
            }

            return sb.ToString();
        }

        private static bool IsAscii(char c)
        {
            return c <= sbyte.MaxValue;
        }
    }
}