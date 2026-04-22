fun manifestNamespace(projectDir: File): String? {
    val manifestFile = projectDir.resolve("src/main/AndroidManifest.xml")
    if (!manifestFile.exists()) {
        return null
    }

    return Regex("""package\s*=\s*"([^"]+)"""")
        .find(manifestFile.readText())
        ?.groupValues
        ?.getOrNull(1)
}

fun fallbackNamespace(projectName: String): String {
    val sanitizedName = buildString {
        for (character in projectName) {
            append(
                when {
                    character.isLetterOrDigit() -> character.lowercaseChar()
                    else -> '_'
                }
            )
        }
    }

    return "com.roadguard.$sanitizedName"
}

fun Project.applyAndroidNamespaceFallback() {
    val androidExtension = extensions.findByName("android") ?: return
    val getNamespace = androidExtension.javaClass.methods.firstOrNull {
        it.name == "getNamespace" && it.parameterCount == 0
    } ?: return
    val setNamespace = androidExtension.javaClass.methods.firstOrNull {
        it.name == "setNamespace" && it.parameterCount == 1
    } ?: return

    val currentNamespace = getNamespace.invoke(androidExtension) as? String
    if (!currentNamespace.isNullOrBlank()) {
        return
    }

    val namespace = manifestNamespace(projectDir) ?: fallbackNamespace(name)
    setNamespace.invoke(androidExtension, namespace)
}

allprojects {
    repositories {
        google()
        mavenCentral()
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)

    pluginManager.withPlugin("com.android.application") {
        applyAndroidNamespaceFallback()
    }

    pluginManager.withPlugin("com.android.library") {
        applyAndroidNamespaceFallback()
    }
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}
