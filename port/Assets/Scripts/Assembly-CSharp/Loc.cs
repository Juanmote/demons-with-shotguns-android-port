using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

// Port Android: traducción al castellano. Solo cambia lo que se dibuja en pantalla
// (tk2dTextMesh.FormatText); la lógica del juego sigue viendo el texto original en inglés.
public static class Loc
{
	private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

	private const string LanguageKey = "DwsLanguage";

	private static int _spanish = -1;

	// Idioma elegido en Opciones. Por defecto, castellano.
	public static bool Spanish
	{
		get
		{
			if (_spanish < 0) _spanish = UnityEngine.PlayerPrefs.GetInt(LanguageKey, 1);
			return _spanish == 1;
		}
	}

	public static void SetSpanish(bool spanish)
	{
		_spanish = spanish ? 1 : 0;
		UnityEngine.PlayerPrefs.SetInt(LanguageKey, _spanish);
		UnityEngine.PlayerPrefs.Save();
		Cache.Clear();
		tk2dTextMesh[] meshes = UnityEngine.Resources.FindObjectsOfTypeAll<tk2dTextMesh>();
		for (int i = 0; i < meshes.Length; i++)
		{
			if (meshes[i] != null && meshes[i].gameObject.scene.IsValid()) meshes[i].RefreshLoc();
		}
	}

	// Texto de la opción del menú (sustituye a "Fullscreen", que en Android no sirve).
	public static string OptionLabel()
	{
		return Spanish ? "Idioma: Español" : "Language: English";
	}

	private static readonly Regex Label = new Regex("^([^:]+:)\\s*(.*)$");
	private static readonly Regex OnOff = new Regex("^(.+): (On|Off)$");
	private static readonly Regex Duration = new Regex("^(\\d+) (Minute|Second)(s?)$");
	private static readonly Regex Wave = new Regex("^Wave (\\d+) of (\\d+)$");
	private static readonly Regex Souls = new Regex("^Captured (\\d+) souls!$");
	private static readonly Regex Captured = new Regex("^(.+) CAPTURED THE SOUL!$");
	private static readonly Regex HasSoul = new Regex("^(.+) HAS THE SOUL!$");
	private static readonly Regex Rebind = new Regex("^Press (Button|Key) for (.+)$");

	private static readonly Dictionary<string, string> Es = new Dictionary<string, string>
	{
		// Generales
		{ "On", "Sí" }, { "Off", "No" }, { "Back", "Volver" }, { "Exit", "Salir" }, { "Continue", "Continuar" },
		{ "Resume", "Reanudar" }, { "Paused", "Pausa" }, { "Options", "Opciones" }, { "options", "opciones" },
		{ "Controls", "Controles" }, { "Credits", "Créditos" }, { "credits", "créditos" }, { "start", "empezar" },
		{ "exit", "salir" }, { "Main Menu", "Menú principal" }, { "Restart", "Reiniciar" }, { "Versus", "Versus" },
		{ "Random", "Aleatorio" }, { "Locked", "Bloqueado" }, { "Ready!", "¡Listo!" }, { "FIGHT!", "¡LUCHA!" },
		{ "GET READY!", "¡PREPÁRATE!" }, { "Press Start to skip", "Pulsa Start para saltar" },
		{ "Press A To Join", "Pulsa A para unirte" }, { "Input device connection lost", "Mando desconectado" },
		{ "Red", "Rojo" }, { "Blue", "Azul" }, { "RED", "ROJO" }, { "BLUE", "AZUL" },
		{ "Game Over", "Fin de la partida" }, { "Completed", "Completado" }, { "Winner", "Ganador" },
		{ "Soulless", "Sin alma" }, { "Rematch", "Revancha" }, { "Arena Select", "Elegir arena" },
		{ "Next Arena", "Siguiente arena" }, { "Next Arena Unlocked!", "¡Siguiente arena desbloqueada!" },
		{ "Unlocked!", "¡desbloqueada!" }, { "New Arena Unlocked!", "¡Nueva arena desbloqueada!" }, { "New Arena", "Nueva arena" },
		{ "Presents", "Presenta" },

		// Pantalla de aviso
		{ "THE FOLLOWING", "EL SIGUIENTE" }, { "GAME", "JUEGO" }, { "HAS BEEN APPROVED FOR", "HA SIDO APROBADO" },
		{ "MATURE AUDIENCES ONLY", "SOLO PARA ADULTOS" }, { "BY MINDSHAFT GAMES, LLC.", "POR MINDSHAFT GAMES, LLC." },

		// Personajes: se quedan todos en inglés (Predicador y Embaucador no cabían en el lobby y el usuario
		// prefiere no mezclar idiomas).

		// Modos de juego
		{ "Select Game Mode", "Elige el modo de juego" }, { "Select Difficulty", "Elige la dificultad" },
		{ "Match Settings", "Ajustes de partida" }, { "Select Tarot Cards", "Elige cartas del tarot" },
		{ "Tarot Cards", "Cartas del tarot" }, { "Normal", "Normal" }, { "Hardcore", "Extremo" },
		{ "A fair challenge.", "Un desafío justo." },
		{ "More waves, more enemies, friendly fire.", "Más oleadas, más enemigos y fuego amigo." },
		{ "End of Times", "Fin de los tiempos" }, { "Deathmatch", "Combate a muerte" },
		{ "Team Deathmatch", "Combate por equipos" }, { "Soul Reaping", "Cosecha de almas" },
		{ "Team Soul Reaping", "Cosecha por equipos" }, { "Capture the Soul", "Captura el alma" },
		{ "Team Capture the Soul", "Captura por equipos" }, { "Last Soul Standing", "Última alma en pie" },
		{ "King of the Soul", "Rey del alma" }, { "Team King of the Soul", "Rey por equipos" },
		{ "Classic Deathmatch. Frag all other players. First player to 15 frags wins.", "Combate a muerte clásico. Mata a los demás jugadores. Gana el primero que consiga 15 muertes." },
		{ "Classic Team Deathmatch. Each team collectively frags all other players. First team to 15 frags wins.", "Combate a muerte clásico por equipos. Cada equipo suma las muertes de sus jugadores. Gana el primero que consiga 15." },
		{ "Frag all other players to release their soul and collect it. First player to collect 10 souls wins. Be warned, for when you have no more souls, you can no longer respawn!", "Mata a los demás jugadores para liberar su alma y recógela. Gana el primero que reúna 10 almas. Cuidado: cuando te quedes sin almas, ya no podrás reaparecer." },
		{ "Frag all other players to release their soul and collect it. First player to collect 10 souls wins. Be wanred, for when you have no more souls, you can no longer respawn!", "Mata a los demás jugadores para liberar su alma y recógela. Gana el primero que reúna 10 almas. Cuidado: cuando te quedes sin almas, ya no podrás reaparecer." },
		{ "Frag all other players to release their soul and collect it. A team shares a pool of souls. First team to collect 10 souls wins.", "Mata a los demás jugadores para liberar su alma y recógela. Cada equipo comparte sus almas. Gana el primero que reúna 10." },
		{ "Frag a player to release their soul and bring it back to the capture base. Only one soul can be active at any given time. First player to 5 captures wins.", "Mata a un jugador para liberar su alma y llévala a la base de captura. Solo puede haber un alma activa a la vez. Gana el primero que haga 5 capturas." },
		{ "Frag a player to release their soul and bring it back to the capture base. Only one soul can be active at any given time. First team to 5 captures wins.", "Mata a un jugador para liberar su alma y llévala a la base de captura. Solo puede haber un alma activa a la vez. Gana el primer equipo que haga 5 capturas." },
		{ "Each player only has one life. Last player standing wins the round. First player to 10 rounds wins.", "Cada jugador tiene una sola vida. El último en pie gana la ronda. Gana la partida el primero en ganar 10 rondas." },
		{ "Grab hold of the single soul and fight to maintain it. Hold onto the soul for 5 seconds and earn a point. First to 10 points wins.", "Coge la única alma y lucha por conservarla. Cada 5 segundos con ella ganas un punto. Gana el primero que llegue a 10." },
		{ "Grab hold of the single soul and fight to maintain it. Hold onto the soul for 5 seconds and earn a point. First team to 10 points wins.", "Coge la única alma y lucha por conservarla. Cada 5 segundos con ella ganas un punto. Gana el primer equipo que llegue a 10." },

		// Ajustes de partida
		{ "Time Limit:", "Tiempo límite:" }, { "Frag Limit:", "Límite de muertes:" }, { "Capture Limit:", "Límite de capturas:" },
		{ "Soul Limit:", "Límite de almas:" }, { "Number of Rounds:", "Número de rondas:" }, { "Time Per Round:", "Tiempo por ronda:" },
		{ "Ammo Spawn:", "Munición:" }, { "Tarot Spawn:", "Cartas:" }, { "Player Spawn:", "Reaparición:" },
		{ "Shields:", "Escudos:" }, { "Friendly Fire:", "Fuego amigo:" }, { "Tarot Cards:", "Cartas del tarot:" },

		// Cartas del tarot
		{ "Speed Boost:", "Velocidad:" }, { "Satan's Spawn:", "Hijo de Satán:" }, { "Angelic:", "Angelical:" },
		{ "Holy Spirit:", "Espíritu Santo:" }, { "Possessed:", "Poseído:" }, { "Power of Christ:", "Poder de Cristo:" },
		{ "Crown of Thorns:", "Corona de espinas:" }, { "Gluttonous:", "Glotón:" }, { "Sands of Time:", "Arenas del tiempo:" },
		{ "Plague:", "Plaga:" }, { "In Thy Image:", "A tu imagen:" },
		{ "Increases your movement speed.", "Aumenta tu velocidad de movimiento." }, { "Instagib kills!", "¡Muertes de un solo golpe!" },
		{ "Gives you angel wings, increasing your jump height.", "Te da alas de ángel que aumentan la altura de tu salto." },
		{ "The power of the holy spirit makes you invincible.", "El poder del Espíritu Santo te hace invencible." },
		{ "The demon within you forces you to auto fire your shotgun.", "El demonio que llevas dentro te obliga a disparar sin parar." },
		{ "The power of Christ compels you, giving you unlimited holy water.", "El poder de Cristo te obliga y te da agua bendita ilimitada." },
		{ "Snares your enemy in a crown of thorns, immobilizing them.", "Atrapa a tus enemigos en una corona de espinas y los inmoviliza." },
		{ "Reduces the motor skills of your drunk enemies.", "Reduce la coordinación de tus enemigos borrachos." },
		{ "Increases your temporal awareness and slows down time.", "Aumenta tu percepción temporal y ralentiza el tiempo." },
		{ "A mighty plague that drains the health of your enemies.", "Una plaga poderosa que drena la salud de tus enemigos." },
		{ "Resurrect the dead in your image to fight for you.", "Resucita a los muertos a tu imagen para que luchen por ti." },

		// Avisos durante la partida
		{ "Frag them all", "Mátalos a todos" }, { "Collect their souls", "Recoge sus almas" }, { "Control the soul", "Controla el alma" },
		{ "Capture the soul", "Captura el alma" }, { "BLACK SOULS", "ALMAS NEGRAS" }, { "CROWN OF THORNS", "CORONA DE ESPINAS" },
		{ "PLAGUE", "PLAGA" }, { "APOCALYPSE", "APOCALIPSIS" }, { "KING", "REY" }, { "KING OF KINGS", "REY DE REYES" },
		{ "MESSIAH", "MESÍAS" }, { "ANTICHRIST", "ANTICRISTO" }, { "Antichrist", "Anticristo" }, { "DOUBLE KILL", "DOBLE MUERTE" },
		{ "MULTI KILL", "MULTIMUERTE" }, { "MEGA KILL", "MEGAMUERTE" }, { "MONSTER KILL", "MUERTE MONSTRUOSA" },
		{ "KING SLAYER", "MATARREYES" }, { "TOASTY", "¡TOSTADO!" }, { "POSSESSED", "POSEÍDO" }, { "CRUSHED", "APLASTADO" },
		{ "GLUTTONOUS", "GLOTÓN" }, { "SATAN'S SPAWN", "HIJO DE SATÁN" }, { "SANDS OF TIME", "ARENAS DEL TIEMPO" },
		{ "IN THY IMAGE", "A TU IMAGEN" }, { "SUDDEN DEATH!", "¡MUERTE SÚBITA!" }, { "TELEFRAG", "TELEMUERTE" },
		{ "HOLY SPIRIT", "ESPÍRITU SANTO" }, { "ANGELIC", "ANGELICAL" }, { "SPEED BOOST", "VELOCIDAD" }, { "POWER OF CHRIST", "PODER DE CRISTO" },
		{ "Full health recharge!", "¡Salud recargada al máximo!" },

		// Estadísticas
		{ "Accuracy:", "Puntería:" }, { "Kills:", "Muertes:" }, { "Deaths:", "Caídas:" }, { "Damage:", "Daño:" },
		{ "Total score:", "Puntos totales:" }, { "Time:", "Tiempo:" }, { "Shots", "Disparos" }, { "Fired:", "Hechos:" },
		{ "Hit:", "Aciertos:" }, { "Blocked:", "Bloqueados:" },

		// Opciones y controles
		{ "Film Grain", "Grano de película" }, { "Auto Skip Get Ready", "Saltar «Prepárate»" }, { "Gamepad Vibration", "Vibración del mando" },
		{ "Controller Vibration", "Vibración del mando" }, { "End of Times HUD", "Marcador Fin de los tiempos" },
		{ "Camera Shake", "Temblor de cámara" }, { "Fullscreen", "Pantalla completa" }, { "Music Volume:", "Volumen de música:" },
		{ "Sound Effects Volume:", "Volumen de efectos:" }, { "Music:", "Música:" }, { "Sound:", "Sonido:" },
		{ "Default Controls", "Controles por defecto" }, { "Reset All Bindings", "Restablecer controles" },
		{ "Rebind Gamepad Controls", "Cambiar controles del mando" }, { "Rebind Keyboard Controls", "Cambiar controles del teclado" },
		{ "Move/Aim", "Mover/Apuntar" }, { "Charge melee", "Cargar golpe" }, { "Shield", "Escudo" }, { "Dash", "Embestida" },
		{ "Holy Water", "Agua bendita" }, { "Jump", "Saltar" }, { "Shoot", "Disparar" }, { "Grenade", "Granada" }, { "Melee", "Golpe" },
		{ "Taunt", "Burla" }, { "Pause", "Pausa" }, { "Aim Up", "Apuntar arriba" }, { "Aim Down", "Apuntar abajo" },
		{ "Move Left", "Izquierda" }, { "Move Right", "Derecha" },

		// Tutorial
		{ "Double Jump", "Doble salto" }, { "Pressing the jump button twice will let you perform a double jump.", "Pulsa dos veces el botón de salto para hacer un doble salto." },
		{ "Shotgun Jumping", "Salto con escopeta" }, { "While jumping, aim down and shoot to get an extra jump boost. Try this while double jumping.", "Mientras saltas, apunta abajo y dispara para impulsarte más. Pruébalo con el doble salto." },
		{ "Blocking", "Bloqueo" }, { "Hold up your shield to deflect shots back at your opponent.", "Levanta el escudo para devolver los disparos a tu rival." },
		{ "Dashing", "Embestida" }, { "Dash to escape or close the gap between your opponent.", "Embiste para escapar o acercarte a tu rival." },
		{ "Holy Spirit", "Espíritu Santo" }, { "Power of the holy spirit makes you invincible.", "El poder del Espíritu Santo te hace invencible." },
		{ "Become invincible with the Holy Spirit Tarot Card.", "Vuélvete invencible con la carta del Espíritu Santo." },
		{ "Possessed", "Poseído" }, { "The devil inside you allows you increased firing rate.", "El diablo que llevas dentro aumenta tu cadencia de disparo." },
		{ "Apocalypse", "Apocalipsis" }, { "At the end of times, everyone dies.", "En el fin de los tiempos, todos mueren." },
		{ "Black Souls", "Almas negras" }, { "Souls so black are undesirable and strip you of points.", "Las almas tan negras no interesan y te quitan puntos." },
		{ "Satan's Spawn", "Hijo de Satán" }, { "Being the spawn of Satan turns you into a instagib death machine.", "Ser hijo de Satán te convierte en una máquina de matar de un solo golpe." },
		{ "Angelic", "Angelical" }, { "Reach new heights with your angel wings.", "Alcanza nuevas alturas con tus alas de ángel." },
		{ "Power of Christ", "Poder de Cristo" }, { "Compels you to spam your holy water grenade.", "Te obliga a lanzar granadas de agua bendita sin parar." },
		{ "Crown of Thorns", "Corona de espinas" }, { "A crown so heavy weighs you down, preventing you to move.", "Una corona tan pesada que no te deja moverte." },
		{ "Gluttonous", "Glotón" }, { "Had too much to drink? Good luck with those motor skills and reversed controls.", "¿Has bebido de más? Suerte con esa coordinación y los controles invertidos." },
		{ "Sands of Time", "Arenas del tiempo" }, { "Time is master to no one.", "El tiempo no tiene amo." },
		{ "Plague", "Plaga" }, { "An epidemic of biblical proportions, draining everyone to 1 health point.", "Una epidemia de proporciones bíblicas que deja a todos con 1 punto de salud." },

		// Consejos
		{ "Aim down while shooting to perform a shotgun jump. Try combining this with a double jump!", "Apunta abajo al disparar para hacer un salto con escopeta. ¡Combínalo con el doble salto!" },
		{ "Use your shield to deflect bullets back at your opponent and earn a frag.", "Usa el escudo para devolver las balas a tu rival y conseguir una muerte." },
		{ "Charge up your melee attack for a powerful shield bash. Use wisely, as you'll be stunned if you miss.", "Carga tu golpe para dar un potente escudazo. Úsalo con cabeza: si fallas, te quedarás aturdido." },
		{ "When the Plague hits your opponent's, their health will drain, making them very vulnerable.", "Cuando la Plaga alcanza a tus rivales, su salud se drena y quedan muy vulnerables." },
		{ "As Satan's Spawn, you'll be transformed into an instagib killing machine.", "Como hijo de Satán, serás una máquina de matar de un solo golpe." },
		{ "The Cemetery has lots of hidden pathways and hideaways.", "El cementerio tiene muchos pasadizos y escondites." },
		{ "Make a long distance frag by shooting through a portal in Hell.", "Consigue una muerte a distancia disparando a través de un portal en el Infierno." },
		{ "When Possessed, you'll lose control, firing your weapon at a frantic rate.", "Si estás poseído, perderás el control y dispararás a un ritmo frenético." },
		{ "Your shield has limited use before splintering into pieces. Use wisely.", "Tu escudo acabará haciéndose astillas. Úsalo con cabeza." },
		{ "There's no stopping the explosive power of holy water. Watch out!", "Nada detiene el poder explosivo del agua bendita. ¡Cuidado!" },
		{ "When the Power of Christ compels you, you have infinite holy water.", "Cuando el poder de Cristo te obliga, tienes agua bendita infinita." },
		{ "Skull blocks at the Cemetery are bad. Avoid them!", "Los bloques de calaveras del cementerio son malos. ¡Evítalos!" },
		{ "Elevators will crush you. Mind your head!", "Los ascensores te aplastarán. ¡Cuidado con la cabeza!" },
		{ "A bullet graze won't kill you. A buckshot to the chest will!", "Un roce de bala no te matará. ¡Un perdigonazo en el pecho sí!" },
		{ "You'll have nothing to fear when the power of the Holy Spirit makes you invincible.", "No temerás nada cuando el poder del Espíritu Santo te haga invencible." },
		{ "Out of ammo? Grab an ammo pack or use your melee attack.", "¿Sin munición? Coge un paquete de balas o usa tu golpe." },
		{ "Keep your distance from Bloody Marys and Hellhounds.", "Mantén las distancias con las Marías Sangrientas y los perros del infierno." },
		{ "Use your shield to block and deflect enemy shots.", "Usa el escudo para bloquear y devolver los disparos enemigos." },
		{ "Don't try to block a rocket. It won't turn out well.", "No intentes bloquear un cohete. No acabará bien." },
		{ "Can't reach a platform? Try double jumping or shotgun jumping.", "¿No llegas a una plataforma? Prueba con el doble salto o el salto con escopeta." },
		{ "Capture the souls of your enemies to earn a 1UP.", "Captura las almas de tus enemigos para ganar una vida." },
		{ "Gluts are easy to kill, but explosive. Watch out!", "Los glotones son fáciles de matar, pero explotan. ¡Cuidado!" },
		{ "Enemies are temporarily invincible when they're spawning.", "Los enemigos son invencibles durante un momento al aparecer." },
		{ "When he's throwing holy water, he's desperate.", "Cuando lanza agua bendita, es que está desesperado." },
		{ "If you can stun him, he'll be open to attack.", "Si lo aturdes, quedará expuesto a tus ataques." },
		{ "Stay frosty between waves, capture souls and earn a 1UP.", "Mantén la calma entre oleadas, captura almas y gana una vida." },
		{ "Enemy projectiles also pass through portals. Watch out!", "Los proyectiles enemigos también cruzan los portales. ¡Cuidado!" },
		{ "The sheep shot at his feet and was lifted to great heights.", "La oveja disparó a sus pies y se elevó a grandes alturas." },
		{ "Austin 5:43", "Austin 5:43" },

		// Arenas
		{ "Death's Labyrinth", "Laberinto de la Muerte" }, { "Lonely Bones", "Huesos solitarios" }, { "Guarded Secret", "Secreto guardado" },
		{ "Sinkhole", "Socavón" }, { "Spike Pit", "Foso de pinchos" }, { "House of Moss", "Casa de musgo" }, { "Closed Chambers", "Cámaras cerradas" },
		{ "Crosses and Cubes", "Cruces y cubos" }, { "Pain and Suffering", "Dolor y sufrimiento" }, { "Maze of Horrors", "Laberinto de horrores" },
		{ "Abstract Sin", "Pecado abstracto" }, { "Split Caverns", "Cavernas partidas" }, { "Shattered Reflection", "Reflejo roto" },
		{ "You Can Run...", "Puedes correr..." }, { "Seven Deadly Sins", "Siete pecados capitales" }, { "One Way", "Sentido único" },
		{ "Mushroom Cave", "Cueva de setas" }, { "...But You Can't Hide", "...pero no esconderte" }, { "Tunnels of Hate", "Túneles de odio" },
		{ "Eternal Insanity", "Locura eterna" }, { "Package Insurance", "Seguro de paquetes" }, { "Rampage", "Arrasa" }, { "Safety Net", "Red de seguridad" },
		{ "Service Elevator", "Montacargas" }, { "Glass?", "¿Cristal?" }, { "Spotlight Terror", "Terror en el foco" }, { "Long Way Down", "Larga caída" },
		{ "Claustrophobia", "Claustrofobia" }, { "Hard Hat Hazard", "Zona de obras" }, { "Co-op Shootout", "Tiroteo cooperativo" }, { "Redemption", "Redención" },
		{ "Blood Stained Glass", "Vidriera de sangre" }, { "Altar of War", "Altar de guerra" }, { "Forgotten Sacrifice", "Sacrificio olvidado" },
		{ "No Mercy, No Faith", "Sin piedad, sin fe" }, { "Shotgun Blast to Heaven", "Escopetazo al cielo" }, { "Our Father", "Padre nuestro" },
		{ "Last Rites", "Últimos ritos" }, { "Renounce Satan", "Renuncia a Satán" }, { "Mother's Basement", "Sótano de mamá" },
		{ "Ghosts n' Nomeds", "Fantasmas y nómadas" }, { "Water Tower Terror", "Terror en la torre" }, { "At The Gates", "A las puertas" }, { "Sacrament", "Sacramento" },
		{ "Cemetery", "Cementerio" }, { "Metro", "Metro" }, { "Church", "Iglesia" }, { "Hell", "Infierno" },

		// Créditos
		{ "MindShaft Games is:", "MindShaft Games son:" }, { "Presley, the game developer dog!", "¡Presley, el perro desarrollador!" },
		{ "Artwork and additional design by:", "Arte y diseño adicional:" }, { "Additional artwork by:", "Arte adicional:" },
		{ "Concept and promotional art by:", "Arte conceptual y promocional:" }, { "Music by:", "Música:" }, { "Voice over by:", "Voces:" },
		{ "Ported by Juanmote", "Port de Juanmote" }, { "Sound effects used:", "Efectos de sonido usados:" },
		{ "Changes were made to these sound effects.", "Estos efectos de sonido se han modificado." },
	};

	// Devuelve el texto en castellano; lo desconocido (nombres de mandos, números...) queda tal cual.
	public static string T(string source)
	{
		if (string.IsNullOrEmpty(source) || !Spanish) return source;
		string cached;
		if (Cache.TryGetValue(source, out cached)) return cached;
		string result;
		if (source.IndexOf('\n') < 0)
		{
			result = Line(source);
		}
		else
		{
			string[] lines = source.Split('\n');
			StringBuilder sb = new StringBuilder(source.Length + 16);
			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0) sb.Append('\n');
				sb.Append(Line(lines[i]));
			}
			result = sb.ToString();
		}
		if (Cache.Count > 2000) Cache.Clear();
		Cache[source] = result;
		return result;
	}

	private static string Word(string s)
	{
		string es;
		return Es.TryGetValue(s, out es) ? es : s;
	}

	private static string Line(string line)
	{
		string trimmed = line.Trim();
		if (trimmed.Length == 0) return line;
		string es;
		if (Es.TryGetValue(trimmed, out es)) return es;
		bool hasLetter = false;
		for (int i = 0; i < trimmed.Length && !hasLetter; i++) hasLetter = char.IsLetter(trimmed[i]);
		if (!hasLetter) return line;

		Match m = OnOff.Match(trimmed);
		if (m.Success) return Word(m.Groups[1].Value) + ": " + Word(m.Groups[2].Value);
		m = Duration.Match(trimmed);
		if (m.Success)
		{
			string unit = m.Groups[2].Value == "Minute" ? "minuto" : "segundo";
			return m.Groups[1].Value + " " + unit + (m.Groups[3].Value.Length > 0 ? "s" : string.Empty);
		}
		m = Wave.Match(trimmed);
		if (m.Success) return "Oleada " + m.Groups[1].Value + " de " + m.Groups[2].Value;
		m = Souls.Match(trimmed);
		if (m.Success) return "¡" + m.Groups[1].Value + " almas capturadas!";
		m = Captured.Match(trimmed);
		if (m.Success) return "¡" + Word(m.Groups[1].Value) + " HA CAPTURADO EL ALMA!";
		m = HasSoul.Match(trimmed);
		if (m.Success) return "¡" + Word(m.Groups[1].Value) + " TIENE EL ALMA!";
		m = Rebind.Match(trimmed);
		if (m.Success) return "Pulsa " + (m.Groups[1].Value == "Key" ? "tecla" : "botón") + " para: " + Word(m.Groups[2].Value);
		m = Label.Match(trimmed);
		if (m.Success && Es.TryGetValue(m.Groups[1].Value, out es)) return es + " " + m.Groups[2].Value;
		return line;
	}
}
