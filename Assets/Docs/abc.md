# Passo di rifinitura: come il gioco si fa sentire

Il gioco funzionava già: si mira, si spara, gli asteroidi arrivano e la partita finisce.
Quello che mancava era il **ritorno**. Sparavo e non sentivo di aver sparato; prendevo un
colpo e lo scoprivo guardando un numero in alto a sinistra. Questo passo è tutto lì: far
sì che ogni cosa che succede si veda e si senta, senza aggiungere una sola regola nuova al
gioco.

Il filo che tiene insieme i sette punti è uno solo: **chi fa una cosa non decide come si
sente**. La torretta spara e dice che ha sparato; il radar aggancia e dice che ha
agganciato. Come quegli eventi diventano luce, rumore e scosse lo decide un pezzo solo,
il `FeedbackComponent`, montato dentro il Player.

---

## 1. Il post processing, e due effetti tenuti da parte

Ho sistemato la catena di post processing della scena: correzione colore, bloom,
vignetting, panini projection. Non è stato solo un lavoro di look: **alcuni di quegli
effetti li ho scelti pensando di pilotarli a runtime**, il bloom per lo sparo e il
vignetting per il danno.

Ho aggiunto anche un suono d'ambiente di sottofondo e qualche effetto che rende lo sparo
più "chiuso", come se rimbombasse dentro la postazione invece che nel vuoto. E delle
particelle che sembrano stelle che scorrono davanti alla navicella, così anche stando
fermi si ha la sensazione di essere nello spazio e non davanti a un fondale.

**La regola che mi sono dato:** gli effetti dinamici **non toccano mai il Volume della
scena**. Ogni effetto che pulsa vive in un Volume suo, dentro il prefab del Player, con
priorità più alta. Il codice ne muove soltanto il **peso**, da 0 a 1, e Unity miscela da
solo fra l'aspetto della scena e quello del profilo.

Tre vantaggi, ed è il motivo per cui l'ho fatto così:
- il profilo della scena resta un asset che posso ritoccare quando voglio, e non viene
  mai riscritto dal gioco;
- posso metterci dentro quello che voglio, e tutto pulsa insieme;
- il feedback **viaggia col prefab del Player**: se un domani nasce una seconda modalità
  di gioco, arriva già montato.

---

## 2. Il modello, le texture, l'emissiva

Ho messo in scena il modello della navicella con le sue texture, e ho lavorato
sull'**emissiva**: i pannelli, le spie e le parti illuminate della postazione. Con il
bloom acceso l'emissiva non è decorazione, è quello che fa sembrare la cabina accesa
invece che verniciata.

---

## 3. Lo sparo: ogni colpo accende lo schermo

Ogni colpo fa salire il bloom e poi lo fa tornare giù seguendo una curva. **Il cannone
arriva al massimo, la mitraglietta a poco meno della metà**: il colpo pesante deve
sentirsi più del colpo leggero, e la differenza si vede prima ancora di guardare l'arma
in mano.

Il problema vero è stato la raffica. La mitraglietta spara dieci colpi al secondo: se
ogni colpo facesse ripartire il bagliore da zero, la raffica diventerebbe uno
stroboscopio. Così ogni colpo apre un **impulso** per conto suo, e a schermo arriva
sempre il più alto fra quelli ancora vivi. Risultato: durante la raffica il bagliore
resta alto invece di sfarfallare, e un colpo di cannone non viene spento da un colpo di
mitraglietta sparato subito dopo.

Nello stesso profilo c'è anche la panini projection, che si apre insieme al bagliore.

---

## 4. Il suono: capire l'arma senza guardare l'interfaccia

**Il pitch della mitraglietta sale col calore.** Ogni colpo ha un tono che parte da
quello base a canna fredda e sale fino a un massimo quando l'arma sta per bloccarsi, più
uno scarto casuale in più o in meno che impedisce alla raffica di suonare come un
campione ripetuto. Il punto non è l'estetica: è che **senti arrivare il surriscaldamento
prima che succeda**, e molli il grilletto in tempo senza dover guardare la barra.

Poi ho dato una voce agli eventi che prima passavano in silenzio:
- il **radar** che aggancia un corpo in rotta di collisione;
- il **cambio arma**, con un suono per la mitraglietta e uno per il cannone;
- la mitraglietta che va in **overheat**;
- il **cannone che finisce di ricaricare**.

Sul radar c'è una scelta che vale la pena raccontare: il suono parte **una volta per
spazzata**, non una per contatto. Il radar spazza a intervalli, e in una spazzata può
scoprire tre sassi insieme: tre suoni identici nello stesso istante sarebbero un rumore
sporco, non un'informazione.

---

## 5. Le armi: capire al volo cosa si può usare

Prima calore e ricarica erano due righe nel terminale in alto a sinistra, cioè
**dall'altra parte dello schermo rispetto a dove si guarda**. Mentre spari guardi il
mirino, e per sapere se il cannone era pronto dovevi staccare gli occhi dal bersaglio.

Ho spostato quelle due informazioni **addosso al mirino**: due barre verticali che lo
seguono, il calore a sinistra e la carica del cannone a destra. Il calore passa da verde
ad ambra e poi a rosso quando l'arma si blocca. Le barre **compaiono solo quando hanno
qualcosa da dire** — calore sopra zero, cannone in ricarica — così a canna fredda e
cannone carico il mirino resta pulito. Dal terminale ho tolto le righe corrispondenti,
compresa quella dell'arma in mano, che il reticolo già dichiara cambiando forma.

Quando il cannone torna carico compare una **notifica in basso al centro**, che entra dal
basso con un piccolo rimbalzo e poi sfuma. È l'unica informazione che non poteva essere
una barra: "il cannone è tornato pronto" non è una condizione da ridisegnare a ogni
frame, è un **fatto** che succede in un istante preciso, e va detto in quell'istante.

Nello stesso passo ho cambiato anche una regola di gioco, perché senza di essa gli
indicatori restavano ambigui: **dopo un colpo di cannone si torna da soli alla
mitraglietta**, e il cannone non si può riselezionare finché non è di nuovo carico.
Prima era possibile restare con in mano un'arma che non poteva sparare, il che è solo un
modo per far premere il tasto a vuoto.

---

## 6. Il danno: lo schermo che si chiude

Il vignetting rosso adesso racconta due cose insieme:

- **quanto sei messo male** — una quantità che resta addosso e cresce con la vita persa,
  seguendo una curva regolabile: a metà vita, metà vignetting;
- **il colpo appena preso** — un impulso che si somma sopra quella base e poi ci torna,
  sempre con una curva.

È lo stesso schema del pitch della mitraglietta: una **base**, che è una condizione, e un
**impulso**, che è un fatto. L'impulso si somma sempre sopra la base, qualunque essa sia,
quindi un colpo si sente anche quando sei già ridotto male — parte solo da più in alto.

Il colore del vignetting resta quello della scena: il profilo del danno sovrascrive
soltanto **quanto è forte**. Così il rosso si decide in un posto solo.

---

## 7. I colpi si vedono

Il colpo, nel codice, è istantaneo: un raggio che parte dalla camera e arriva nello stesso
frame. Quello che si vede quindi non è il proiettile, è il suo **racconto**: parte dalla
bocca della canna e arriva dove il colpo ha preso, o, se è andato a vuoto, sul punto di
mira. Così converge sempre dove hai puntato.

- **Mitraglietta:** un tracciante veloce per ogni colpo, una particella stirata nella
  direzione del moto. La sua durata è calcolata sulla distanza, così si spegne sul
  bersaglio invece di attraversarlo.
- **Cannone:** un raggio che compare intero dalla canna al bersaglio e poi si dissolve,
  con un nucleo chiaro e un alone morbido.

Tutti e due usano materiali additivi con colori oltre il bianco, quindi **passano la
soglia del bloom**: il tracciante non è una riga colorata, è una riga che illumina. È lo
stesso bloom del punto 1, usato una terza volta.

Gli effetti non sono costruiti a mano nell'Inspector ma da uno **strumento nel menu
dell'Editor**, che genera materiali, texture e prefab. Rilanciandolo si rigenerano
identici, e le impostazioni stanno scritte in un file invece che dentro un asset binario.

---

## 8. Gli asteroidi più grossi

Ho raddoppiato la scala degli asteroidi. A schermo si leggono molto meglio: si distingue
la sagoma mentre arriva, si capisce da che parte sta girando, e sparargli addosso diventa
una cosa che si fa guardando il sasso invece del marcatore che ha attorno.

La parte interessante è che **il resto non ha avuto bisogno di essere ritoccato**. Ogni
asteroide dichiara la propria stazza misurandosela dal collider della forma che ha
acceso in quel momento, scala compresa. La previsione d'impatto e i marcatori sul mirino
leggono quel numero, quindi si sono adeguati da soli. Se avessi scritto la stazza a mano
da qualche parte, adesso ci sarebbe una seconda verità da ricordarsi di aggiornare a ogni
cambio di modello.
