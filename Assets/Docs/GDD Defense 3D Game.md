# Game Design Document — Asteroid Defense

## 1. Overview

- **Titolo:** Asteroid Defense
- **Genere:** Arcade Defense / Survival 3D
- **Visuale:** Prima persona da postazione fissa
- **Sessione tipo:** pochi minuti, rigiocabile all'istante

In Asteroid Defense il giocatore siede alla postazione di tiro di una piccola
navicella sospesa nello spazio e deve difenderla da un flusso continuo di asteroidi.

Gli asteroidi arrivano dal cielo davanti alla navicella. Non tutti sono diretti
contro di lei: alcuni sono una minaccia, altri passano soltanto e sono un'occasione
per fare punti. Il gioco dice al giocatore quali sono pericolosi, e fra quanto
arrivano, così che possa decidere a cosa sparare prima.

Il giocatore dispone di due armi con comportamenti opposti — una raffica che si
surriscalda e un colpo singolo che ricarica — e deve alternarle per non restare mai
senza fuoco.

La partita termina quando lo scafo della navicella è esaurito. Non esiste una
vittoria: la pressione cresce fino a quando il giocatore cede.

L'obiettivo è fare più punti possibile e sopravvivere più a lungo possibile. Sono
due traguardi distinti, ed entrambi vengono ricordati.

---

## 2. Game Concept

Il giocatore è fermo. La navicella non si sposta e non si può schivare: l'unica
difesa è il fuoco della torretta, che ruota liberamente per seguire la mira.

Durante la partita:

- gli asteroidi compaiono in lontananza davanti alla navicella e le vengono incontro;
- una parte di loro è in rotta di collisione, il resto passa a lato;
- gli asteroidi pericolosi vengono marcati a schermo con un conto alla rovescia;
- il giocatore li abbatte con le due armi della torretta;
- ogni asteroide abbattuto vale punti, pericoloso o no;
- gli asteroidi che raggiungono lo scafo lo danneggiano e si disintegrano senza dare punti;
- col passare del tempo gli asteroidi arrivano più spesso, più veloci e più mirati.

La tensione del gioco sta nella scelta: sparare a ciò che fa punti o a ciò che fa male.
All'inizio si può fare entrambe le cose. Verso la fine no.

---

## 3. Core Loop

1. Il giocatore entra in partita: la navicella è integra, le armi cariche.
2. Gli asteroidi cominciano a comparire davanti alla navicella.
3. Il giocatore mira con la torretta e osserva quali asteroidi sono marcati come pericolosi.
4. Sceglie l'arma adatta: raffica per i sassi lontani, cannone per il colpo che deve andare a segno.
5. Abbatte gli asteroidi e accumula punti.
6. Quando serve il colpo sicuro prende il cannone: sparato quello, la torretta rimette in mano la mitraglietta da sola.
7. Gli asteroidi che sfuggono e colpiscono lo scafo tolgono un punto di resistenza.
8. Il livello di minaccia sale e con lui il ritmo, la velocità e la precisione degli asteroidi.
9. A scafo esaurito la partita finisce.
10. La schermata finale mostra punteggio e tempo di sopravvivenza, confrontati con i record.
11. Il giocatore ricomincia subito oppure torna al menu.

---

## 4. Game Flow

```
MAIN MENU ──[Play]──▶ PLAYING ──[scafo a 0]──▶ GAME OVER ──[Retry]──▶ PLAYING
    ▲                                              │
    └──────────────────[Main Menu]─────────────────┘
```

### Main Menu

Lo schermo d'ingresso. Mostra il titolo del gioco e i due record del giocatore:
il miglior punteggio e la sopravvivenza più lunga. Un solo pulsante: **Play**.

### Playing

Premendo Play la partita comincia immediatamente, senza conto alla rovescia o
schermate intermedie: la navicella è già in campo e il primo asteroide è in arrivo.

Durante la partita il giocatore deve:

- osservare il cielo davanti a sé e i marcatori d'impatto;
- decidere a quale asteroide sparare per primo;
- alternare le due armi in base alla loro disponibilità;
- proteggere lo scafo;
- accumulare punti;
- resistere il più a lungo possibile.

### Game Over

La partita termina quando lo scafo arriva a zero. La schermata finale mostra:

- il punteggio della partita, con il miglior punteggio accanto;
- il tempo di sopravvivenza, con il miglior tempo accanto;
- un'etichetta **New Record** vicino a ciascun record appena battuto;
- **Retry**, che ricomincia subito una nuova partita;
- **Main Menu**, che riporta all'ingresso.

Il Retry non passa dal menu: la nuova partita comincia nello stesso istante in cui
si preme il pulsante.

---

## 5. Controls

| Azione | Comando |
|---|---|
| Mirare | Movimento del mouse |
| Sparare | Tasto sinistro del mouse |
| Cambiare arma | Tasto **Q** oppure rotella del mouse |

Il cursore di sistema è nascosto durante la partita: al suo posto c'è il mirino
della torretta. Torna visibile nelle schermate in cui ci sono pulsanti da premere.

Il fuoco si comporta in modo diverso a seconda dell'arma: con la mitraglietta si
**tiene premuto**, con il cannone si **clicca** — un click, un colpo.

Dopo il colpo di cannone la torretta torna da sola alla mitraglietta, che però non
riparte finché il tasto non viene rilasciato: il click del cannone non si porta dietro
una raffica.

---

## 6. Player / Turret

Il giocatore controlla la torretta montata sulla navicella. La vede dalla propria
postazione, in prima persona, con la canna davanti a sé.

### Postazione

La posizione del giocatore è fissa. Non ci si muove, non ci si abbassa, non si scappa.

### Torretta

La torretta segue il mirino:

- ruota orizzontalmente per inseguire la mira;
- alza e abbassa la canna per seguirla in altezza;
- si porta sul bersaglio in fretta ma non di scatto, con un movimento fluido e leggibile;
- spara lungo la linea del mirino: dove sta la croce, lì arriva il colpo.

A inizio partita la torretta è dritta davanti a sé e imbraccia la mitraglietta.

---

## 7. Weapon System

Il giocatore ha due armi. Non sono due versioni della stessa arma: si giocano in modo
diverso e servono a cose diverse.

**La mitraglietta è la posizione di riposo, il cannone è una scelta.** Si prende in mano
solo quando è carico, si usa per un colpo, e subito dopo la torretta rimette in mano la
mitraglietta. Non esiste quindi nessun momento in cui si tiene in mano un'arma che non
può sparare.

| | Mitraglietta | Cannone |
|---|---|---|
| Fuoco | A raffica, tenendo premuto | Un colpo per click |
| Limite | Si surriscalda | Ricarica dopo ogni colpo |
| Come si prende | Sempre disponibile, ci si torna da soli | Solo quando è carico, e per un colpo solo |
| Potenza | Serve più di un colpo per abbattere un asteroide | Abbatte un asteroide in un colpo |
| Uso ideale | Sgranare bersagli lontani, fare volume | Il sasso che sta per arrivare e non può sbagliare |

Nessuna delle due arma va "ricaricata" a mano. Entrambe recuperano da sole, e
continuano a farlo **anche quando non sono in mano**: mentre si usa una, l'altra si
rimette in sesto.

```
MITRAGLIETTA ──[Q, se il cannone è carico]──▶ CANNONE ──[un colpo]──▶ MITRAGLIETTA
                                                                        │
                        (il cannone ricarica in sottofondo, poi è di nuovo prendibile)
```

L'intento è un'alternanza naturale: il giocatore non resta mai disarmato, e la domanda
non è più «quale arma tengo», ma **«è questo il sasso per cui vale la pena spendere il
cannone?»**.

---

## 8. Weapon 1 — Machine Gun

La mitraglietta è l'arma di base: fuoco rapido, tenendo premuto il tasto.

### Comportamento

- spara a raffica con cadenza costante finché il tasto resta premuto;
- ogni colpo aumenta il **calore** dell'arma;
- il calore scende da solo appena si smette di sparare;
- se il calore arriva al massimo, l'arma va in **surriscaldamento** e si blocca;
- dal surriscaldamento si esce solo quando il calore è tornato **completamente a zero**: non basta aspettare un attimo, bisogna lasciarla raffreddare del tutto;
- il raffreddamento prosegue anche mentre il giocatore usa il cannone.

### Ritmo desiderato

Una raffica lunga deve essere possibile, ma tenerla premuta senza pensare deve
avere un prezzo. La mitraglietta premia chi spara a tratti e punisce chi tiene il
dito sul grilletto.

```
RAFFICA → CALORE SALE → RILASCIO → CALORE SCENDE
RAFFICA → CALORE AL MASSIMO → BLOCCATA → RAFFREDDAMENTO COMPLETO → PRONTA
```

### Feedback

- una **barra di calore verticale a sinistra del mirino**, che compare appena l'arma si
  scalda e sparisce quando è di nuovo fredda: arancione da quasi piena, rossa a blocco
  avvenuto;
- il mirino diventa rosso finché non si può sparare;
- il **tono dello sparo sale col calore**: si sente arrivare il blocco prima che arrivi,
  senza bisogno di guardare la barra;
- un suono dedicato nel momento in cui l'arma si blocca.

---

## 9. Weapon 2 — Cannon

Il cannone è l'arma decisiva: un colpo solo, pesante, che abbatte un asteroide al
primo impatto.

### Comportamento

- si spara con un click: un click, un colpo;
- dopo lo sparo entra subito in **ricarica**, e la torretta torna da sola alla mitraglietta;
- mentre ricarica non si può nemmeno prendere in mano: chi preme Q resta sulla mitraglietta;
- la ricarica è breve, ma abbastanza da non poter contare sul cannone per tutto;
- la ricarica prosegue anche mentre il giocatore usa la mitraglietta;
- a inizio partita il cannone è carico.

```
CLICK → COLPO → si torna alla mitraglietta → RICARICA → PRONTO (di nuovo prendibile)
```

### Ruolo nel gioco

Il cannone serve per la minaccia che non si può sbagliare: l'asteroide col conto
alla rovescia più basso. Sparare il cannone a un sasso che passa lontano è uno
spreco, e il gioco vuole che il giocatore lo senta.

### Feedback

- una **barra di carica verticale a destra del mirino**, che compare solo mentre il
  cannone ricarica e sparisce quando il colpo è pronto;
- una **notifica a schermo, in basso al centro, quando il cannone torna carico**,
  accompagnata da un suono: è il momento in cui il giocatore riacquista la scelta, e
  va detto nell'istante in cui succede;
- il mirino cambia forma quando il cannone è in mano;
- il colpo lascia un **raggio** dalla canna al bersaglio, che resta un istante e svanisce.

---

## 10. Weapon Switching

Il giocatore chiama il cannone con **Q** o con la rotella del mouse. Il cambio ha una
condizione sola: **il cannone deve essere carico**. Se non lo è, la richiesta non fa
niente e si resta sulla mitraglietta.

- il cambio è immediato;
- non interrompe né il raffreddamento né la ricarica dell'arma che si lascia;
- dopo il colpo di cannone si torna alla mitraglietta senza premere niente;
- il mirino cambia forma per dire quale arma è in mano: è l'unico posto in cui l'arma
  è scritta, perché il reticolo si guarda già.

Sequenza tipica:

1. Il giocatore sgrana la mitraglietta sui sassi lontani.
2. Un asteroide marcato scende sotto i due secondi.
3. Chiama il cannone e lo abbatte con un colpo.
4. Si ritrova la mitraglietta in mano, che nel frattempo si è raffreddata.
5. Il cannone ricarica in sottofondo e annuncia da solo quando è di nuovo disponibile.

---

## 11. Asteroids

Gli asteroidi sono l'unico nemico del gioco. Ognuno di loro:

- compare in lontananza, nel cielo davanti alla navicella;
- viaggia in linea retta verso un punto del campo attorno alla navicella;
- ruota su sé stesso mentre vola, in modo diverso da tutti gli altri;
- può essere colpito dalle armi;
- si disintegra quando ha subito abbastanza danno, e dà punti;
- se raggiunge lo scafo lo danneggia e si disintegra, senza dare punti;
- se non incontra niente, esce dal campo e sparisce, senza dare punti.

### Pericolosi e no

Non tutti gli asteroidi puntano la navicella. La traiettoria di ciascuno finisce da
qualche parte in un campo attorno alla nave: alcuni la centrano, molti le passano
accanto.

- Un asteroide **in rotta di collisione** è una minaccia: va abbattuto prima che arrivi.
- Un asteroide che **passa a lato** non fa male a nessuno: è un'occasione per fare punti, e ignorarlo non costa niente.

I due valgono gli stessi punti. La differenza è il tempo: l'asteroide pericoloso ha
una scadenza, l'altro no. Il gioco dice chiaramente al giocatore quali sono i primi
(vedi §12), così che la priorità sia una scelta e non un'indovinata.

### Resistenza

Tutti gli asteroidi hanno la stessa resistenza: il cannone li abbatte in un colpo,
la mitraglietta ne ha bisogno di più. Colpirli senza abbatterli produce un feedback
sonoro, così il giocatore sa di essere andato a segno.

### Varietà visiva

Gli asteroidi hanno diverse forme e dimensioni, scelte a caso a ogni comparsa. La
varietà è estetica — regole e resistenza sono identiche — ma **la stazza conta per
l'impatto**: un sasso grosso colpisce lo scafo anche se il suo centro gli passa
vicino, mentre uno piccolo può sfiorarlo senza toccarlo.

---

## 12. Impact Warning

La navicella vede arrivare ciò che la colpirà. Ogni asteroide in rotta di collisione
viene marcato a schermo con:

- un **marcatore** a parentesi che gli resta addosso mentre si muove;
- un **conto alla rovescia** in secondi fino all'impatto.

Il marcatore compare quando l'asteroide entra nella portata della navicella, quindi
un sasso veloce si annuncia da più lontano di uno lento. Il conto alla rovescia
scende in tempo reale, e sotto l'ultimo secondo il marcatore passa da arancione a
**rosso**.

Il marcatore sparisce quando l'asteroide viene abbattuto, quando ha colpito, o
quando è passato senza toccare.

Gli asteroidi che non colpiranno non hanno alcun marcatore: il silenzio è
l'informazione. Se le minacce sono più dei marcatori disponibili, restano a schermo
le più imminenti.

Il marcatore è l'elemento più importante dell'interfaccia: è ciò che trasforma un
cielo pieno di sassi in una lista di priorità.

---

## 13. Spaceship

La navicella è ciò che il giocatore difende. È lo scafo, non il giocatore, a
incassare i colpi.

### Scafo

La navicella ha **5 punti di scafo**.

Quando un asteroide la raggiunge:

- l'asteroide si disintegra;
- lo scafo perde **1 punto**;
- non vengono assegnati punti.

A **0 punti di scafo** la partita termina.

Nessun impatto è mortale da solo: la sconfitta arriva per accumulo, e il giocatore
deve sempre vedere quanto margine gli resta.

### Feedback

Lo stato dello scafo è sempre a schermo, in basso a sinistra, con il nome
**SHIP INTEGRITY**: cinque segmenti che si spengono uno per impatto, e il numero accanto.

| Scafo | Colore |
|---|---|
| 5 – 3 | Verde |
| 2 | Arancione |
| 1 | Rosso |
| 0 | Game Over |

Il numero però non basta, perché sta di lato e si guarda il cielo. Il danno si vede
anche **sui bordi dello schermo, in rosso**, e racconta due cose insieme:

- **quanto scafo manca** — un alone che resta addosso e si fa più fitto man mano che
  la navicella si rovina;
- **il colpo appena preso** — una vampata che sale sopra quell'alone e ci ritorna.

Così un impatto si sente anche quando si sta guardando altrove, e a scafo basso lo
schermo è chiuso abbastanza da ricordarlo senza che si debba leggere niente.

---

## 14. Spawning e Difficulty Progression

Gli asteroidi compaiono senza sosta da un'area del cielo davanti alla navicella,
mai dallo stesso punto esatto, così che le traiettorie non si ripetano.

### Le tre leve

La difficoltà cresce su tre fronti insieme:

1. **Ritmo** — gli asteroidi arrivano sempre più spesso.
2. **Velocità** — viaggiano più in fretta, e la forbice fra i lenti e i veloci cambia.
3. **Mira** — i lanci si addensano sempre più sulla navicella: all'inizio sono
   sparpagliati su tutto il campo, alla fine la maggior parte arriva addosso.

La terza leva è la vera pressione del gioco. Ritmo e velocità fanno il rumore, ma è
quanti asteroidi puntano davvero la nave a decidere quanto si deve sparare, e quanto
poco tempo resta per fare punti sugli altri.

### Progressione

La salita è continua e dura circa **due minuti**. Da lì in poi la difficoltà resta al
massimo fino alla sconfitta.

```
INIZIO ── pochi, lenti, sparsi ── ▶ ── tanti, veloci, addosso ── PRESSIONE MASSIMA
```

### Livello di minaccia

Per rendere leggibile la salita, l'interfaccia mostra un **livello di minaccia da
1 a 5**. È un'indicazione per il giocatore, non uno scalino: la difficoltà cresce
in modo fluido, il numero dice soltanto a che punto della salita ci si trova.

La partita non ha una condizione di vittoria.

---

## 15. Score e Record

### Punteggio

Il giocatore guadagna punti per ogni asteroide **abbattuto con le armi**: 100 punti
l'uno, uguali per tutti.

Non danno punti:

- gli asteroidi che colpiscono lo scafo;
- gli asteroidi che escono dal campo senza essere stati abbattuti.

I punti premiano il fuoco, non la sopravvivenza.

### Tempo di sopravvivenza

Il tempo di partita è sempre a schermo ed è la seconda misura di una buona partita.

### Record

Il gioco ricorda due record, **indipendenti l'uno dall'altro**:

- il **miglior punteggio**;
- il **miglior tempo** di sopravvivenza.

Fare tanti punti in fretta e resistere a lungo sono due modi diversi di giocare
bene: battere uno dei due record non cancella l'altro, e i due valori mostrati
possono venire da partite diverse.

I record sono mostrati nel Main Menu e nella schermata di Game Over. Quando una
partita ne batte uno, la schermata finale lo segnala con l'etichetta **New Record**
accanto al valore.

---

## 16. Game States

### Main Menu

- nessun gameplay;
- record a schermo;
- cursore visibile;
- un solo pulsante: Play.

### Playing

- asteroidi in arrivo;
- mira, fuoco e cambio arma attivi;
- marcatori d'impatto attivi;
- punteggio e tempo che avanzano;
- livello di minaccia che sale;
- scafo vulnerabile;
- cursore nascosto, mirino a schermo.

### Game Over

- asteroidi fermi e rimossi dal campo;
- comandi di gioco disattivati; la torretta resta dove il giocatore l'ha lasciata;
- bilancio della partita a schermo, con i record già aggiornati;
- cursore visibile;
- Retry e Main Menu.

---

## 17. UI

Tutta l'interfaccia è un **terminale di bordo**: testo monospazio verde su fondo
scuro, righe di log precedute da un prompt, stile console. Le tre schermate usano
lo stesso linguaggio, come se fossero tre stati dello stesso terminale.

| Schermata | Intestazione |
|---|---|
| Main Menu | `SHIP TERMINAL // STANDBY` |
| HUD | `SHIP TERMINAL // ONLINE` |
| Game Over | `SIGNAL LOST` |

### Main Menu

- Titolo: **ASTEROID DEFENSE**
- Best Score
- Best Time
- Play

### Gameplay HUD

**Ogni dato ha un posto, scelto in base a quanto spesso lo si guarda.** Un blocco solo
pieno di righe obbliga a scandirle tutte per leggerne una, e mettere lo stato delle armi
lontano dal mirino vuol dire staccare gli occhi dal bersaglio per sapere se si può
premere. Quindi:

| Dove | Cosa | Perché lì |
|---|---|---|
| Addosso al mirino | calore e carica delle armi | si usano mentre si spara, lo sguardo è già lì |
| In basso a sinistra | **Ship Integrity** | si deve vedere senza leggerlo, ma non deve stare in mezzo |
| In alto al centro | **Mission Time** | si consulta, e al centro non compete con nient'altro |
| In alto a destra | **Difficulty** | si consulta di rado, è il più lontano dall'azione |
| In alto a sinistra | **Score**, sotto l'intestazione del terminale | è il dato che conta a fine partita, non durante |

I nomi dicono cosa sono senza bisogno di conoscere il gioco: `T+` è diventato
**MISSION TIME**, `HULL` è diventato **SHIP INTEGRITY**, e `THREAT LVL` è diventato
**DIFFICULTY**: "threat" a schermo era già la parola dei marcatori d'impatto, e usarla
per due cose diverse obbliga a chiedersi ogni volta di quale si stia parlando.

**Le quantità piccole e intere si disegnano a segmenti**, non a barra: i 5 punti di
integrità e i 5 livelli di minaccia sono cinque blocchi che si accendono e si spengono,
con il numero accanto per chi vuole il dato esatto. Contare tre blocchi è più veloce che
valutare quanto è piena una barra. La scala di colore è quella di sempre: verde, ambra
vicino al limite, rosso sull'ultimo gradino.

Sovrapposti al campo di gioco:

- il **mirino**, al posto del cursore, con un reticolo diverso per ciascuna arma, rosso quando l'arma in mano non può sparare;
- le **due barre verticali ai lati del mirino**: a sinistra il calore della mitraglietta, a destra la carica del cannone. Ciascuna compare solo quando ha qualcosa da dire, quindi ad armi in ordine il mirino resta pulito;
- i **marcatori d'impatto** del §12, con il conto alla rovescia;
- le **notifiche**, in basso al centro: una riga che entra, resta un momento e svanisce. Oggi ne esiste una, `CANNON READY`.

L'HUD non ha nulla su cui cliccare e non deve mai coprire il gioco.

### Game Over

- `SIGNAL LOST` e la nota **HULL BREACH — RUN TERMINATED**
- Score, con Best Score sotto e l'eventuale New Record
- Survived, con Best Time sotto e l'eventuale New Record
- Retry
- Main Menu

---

## 18. Level

L'arena è una sola: la navicella sospesa nel vuoto, uno sfondo di stelle, e il cielo
davanti da cui arrivano gli asteroidi.

Elementi:

- il corpo della navicella, con la postazione del giocatore e la torretta sopra;
- lo **scafo vulnerabile**: la parte della navicella che gli asteroidi possono colpire, rivolta verso il cielo da cui arrivano;
- la zona di comparsa degli asteroidi, in lontananza davanti alla navicella;
- il campo attorno alla navicella entro cui finiscono le traiettorie: chi punta il centro colpisce, chi punta il bordo passa.

Non c'è nulla dietro il giocatore, e nulla arriva da dietro: tutto ciò che conta è
davanti.

---

## 19. Visual & Audio Direction

### Visivo

- Modello della navicella e degli asteroidi con mesh vere e materiali semplici, su uno skybox stellato.
- Gli asteroidi ruotano ciascuno a modo proprio: il cielo non deve mai sembrare fermo.
- **Lo sparo si vede.** Ogni colpo di mitraglietta lascia un tracciante che corre fino al bersaglio; il cannone lascia un raggio che resta un istante e svanisce. Tutti e due partono dalla canna e arrivano dove è finito il colpo.
- **Lo schermo reagisce.** Ogni colpo alza il bagliore generale per un istante — il cannone più della mitraglietta — e il danno chiude i bordi di rosso (§13). Sono gli unici due effetti che cambiano nel tempo: tutto il resto dell'immagine è fermo, quindi bastano a farsi notare.
- L'interfaccia è l'unico elemento con colori di stato, sempre gli stessi tre:
  **verde** per ciò che va bene, **arancione** per l'avviso, **rosso** per il pericolo o il blocco.

Ciò che deve restare leggibile a colpo d'occhio, in ordine di importanza:

1. quali asteroidi colpiranno, e fra quanto;
2. se l'arma in mano può sparare adesso;
3. quanto scafo resta;
4. quale arma è in mano;
5. punteggio, tempo, livello di minaccia.

### Audio

- Ogni arma ha il proprio suono di sparo, riconoscibile a orecchio.
- Nessun suono è identico a sé stesso: il tono di ogni colpo varia leggermente, così una raffica non suona come un campione ripetuto.
- **Il suono dice anche lo stato dell'arma:** il tono della mitraglietta sale col calore, e un suono dedicato segna il blocco.
- Hanno una voce anche il **cambio arma**, il **cannone che torna carico** e il **radar che aggancia** un asteroide in rotta di collisione. Quest'ultimo parla per spazzata e non per sasso: tre minacce scoperte insieme sono un avviso, non tre.
- Un asteroide colpito ma non abbattuto emette un suono di impatto.
- Un asteroide abbattuto emette un suono di disintegrazione, nel punto in cui era.
- I suoni non devono interrompersi perché l'asteroide è sparito.
- Un fondo d'ambiente tiene la scena viva anche quando non succede niente.

---

## 20. Game Rules Summary

- Il giocatore controlla una torretta su una navicella ferma, in prima persona.
- La torretta segue il mirino e spara lungo la sua linea.
- Il giocatore ha due armi: una mitraglietta che si surriscalda e un cannone a colpo singolo con ricarica.
- La mitraglietta è l'arma di riposo; il cannone si può prendere in mano solo quando è carico, spara un colpo e poi restituisce la mitraglietta da solo.
- Entrambe le armi recuperano da sole, anche quando non sono in mano.
- La mitraglietta bloccata riparte solo a raffreddamento completo.
- Il cannone abbatte un asteroide in un colpo; la mitraglietta ne ha bisogno di più.
- Gli asteroidi arrivano dal cielo davanti alla navicella, in linea retta.
- Non tutti gli asteroidi puntano la navicella: quelli che la colpiranno sono marcati, con il conto alla rovescia.
- Ogni asteroide abbattuto vale 100 punti, pericoloso o no.
- Gli asteroidi che colpiscono lo scafo o escono dal campo non valgono niente.
- La navicella ha 5 punti di scafo; ogni impatto ne toglie 1.
- A scafo esaurito la partita finisce.
- Ritmo, velocità e mira degli asteroidi crescono insieme per circa due minuti, poi restano al massimo.
- Il livello di minaccia da 1 a 5 è a schermo.
- Il gioco ricorda miglior punteggio e miglior tempo, indipendenti fra loro.
- Non esiste una condizione di vittoria.
