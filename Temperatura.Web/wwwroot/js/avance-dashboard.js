(() => {
  "use strict";

  const dataElement = document.getElementById("datos-graficas-avance");
  const doughnutCanvas = document.getElementById("grafica-avance-general");
  const barsCanvas = document.getElementById("grafica-avance-ambientes");
  if (!dataElement || !doughnutCanvas || typeof Chart === "undefined") {
    return;
  }

  const data = JSON.parse(dataElement.textContent || "{}");
  if (!data.distribucion || !Array.isArray(data.ambientes)) {
    return;
  }

  const labels = [
    "Cumplidos",
    "Regularizados fuera de plazo",
    "Pendientes o sin registro"
  ];
  const colors = ["#2fb344", "#f59f00", "#dfe3e8"];
  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const fontFamily = window.getComputedStyle(document.body).fontFamily;
  const formatNumber = value => new Intl.NumberFormat("es-PE").format(value);
  const formatPercent = value => new Intl.NumberFormat("es-PE", {
    maximumFractionDigits: 2
  }).format(value);

  Chart.defaults.font.family = fontFamily;
  Chart.defaults.color = window.getComputedStyle(document.body)
    .getPropertyValue("--tblr-secondary-color")
    .trim() || "#667382";

  const centerLabelPlugin = {
    id: "dashboardCenterLabel",
    afterDraw(chart, _args, options) {
      if (!options?.display || chart.config.type !== "doughnut") {
        return;
      }

      const { ctx, chartArea } = chart;
      const firstArc = chart.getDatasetMeta(0).data[0];
      const centerX = firstArc?.x ?? (chartArea.left + chartArea.right) / 2;
      const centerY = firstArc?.y ?? (chartArea.top + chartArea.bottom) / 2;
      const outerRadius = firstArc?.outerRadius ?? (chartArea.right - chartArea.left) / 2;
      const endpointLabelY = Math.min(chart.height - 10, centerY + 18);
      const bodyColor = window.getComputedStyle(document.body)
        .getPropertyValue("--tblr-body-color")
        .trim() || "#182433";

      ctx.save();
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillStyle = bodyColor;
      ctx.font = `700 1.75rem ${fontFamily}`;
      ctx.fillText(`${formatPercent(options.percentage)}%`, centerX, centerY - 20);
      ctx.fillStyle = Chart.defaults.color;
      ctx.font = `500 0.75rem ${fontFamily}`;
      ctx.fillText("avance general", centerX, centerY + 5);
      ctx.font = `600 0.65rem ${fontFamily}`;
      ctx.textAlign = "left";
      ctx.fillText("0%", centerX - outerRadius, endpointLabelY);
      ctx.textAlign = "right";
      ctx.fillText("100%", centerX + outerRadius, endpointLabelY);
      ctx.restore();
    }
  };

  const animation = reduceMotion ? false : { duration: 500 };
  const distributionValues = distribution => [
    distribution.cumplidos,
    distribution.fueraDePlazo,
    distribution.pendientes
  ];

  const hasScheduledRecords = data.programados > 0;
  const doughnutLabels = hasScheduledRecords ? labels : ["Sin registros programados"];
  const doughnutValues = hasScheduledRecords
    ? distributionValues(data.distribucion)
    : [1];
  const doughnutColors = hasScheduledRecords ? colors : ["#e2e8f0"];

  new Chart(doughnutCanvas, {
    type: "doughnut",
    data: {
      labels: doughnutLabels,
      datasets: [{
        data: doughnutValues,
        backgroundColor: doughnutColors,
        borderColor: "#ffffff",
        borderWidth: 3,
        hoverOffset: 5
      }]
    },
    plugins: [centerLabelPlugin],
    options: {
      responsive: true,
      maintainAspectRatio: false,
      animation,
      rotation: -90,
      circumference: 180,
      cutout: "72%",
      layout: {
        padding: {
          right: 8,
          bottom: 28,
          left: 8
        }
      },
      plugins: {
        dashboardCenterLabel: {
          display: true,
          percentage: data.porcentajeAvance
        },
        legend: {
          display: false
        },
        tooltip: {
          enabled: hasScheduledRecords,
          callbacks: {
            label(context) {
              const value = context.parsed;
              const percentage = data.programados > 0
                ? value * 100 / data.programados
                : 0;
              return `${context.label}: ${formatNumber(value)} (${formatPercent(percentage)}%)`;
            }
          }
        }
      }
    }
  });

  if (!barsCanvas) {
    return;
  }

  const barContainer = barsCanvas.closest("[data-dashboard-bar-container]");
  if (barContainer) {
    barContainer.style.height = `${Math.max(300, data.ambientes.length * 48 + 110)}px`;
  }

  new Chart(barsCanvas, {
    type: "bar",
    data: {
      labels: data.ambientes.map(item => item.ambiente),
      datasets: labels.map((label, index) => ({
        label,
        data: data.ambientes.map(item => distributionValues(item.distribucion)[index]),
        backgroundColor: colors[index],
        borderWidth: 0,
        borderRadius: 4,
        borderSkipped: false,
        barPercentage: 0.72,
        categoryPercentage: 0.82
      }))
    },
    options: {
      indexAxis: "y",
      responsive: true,
      maintainAspectRatio: false,
      animation,
      interaction: {
        mode: "index",
        intersect: false
      },
      scales: {
        x: {
          stacked: true,
          beginAtZero: true,
          ticks: {
            precision: 0
          },
          title: {
            display: true,
            text: "Registros programados"
          }
        },
        y: {
          stacked: true,
          grid: {
            display: false
          }
        }
      },
      plugins: {
        legend: {
          position: "bottom",
          labels: {
            boxWidth: 12,
            boxHeight: 12,
            padding: 16,
            usePointStyle: true
          }
        },
        tooltip: {
          callbacks: {
            label(context) {
              return `${context.dataset.label}: ${formatNumber(context.parsed.x)}`;
            },
            footer(contexts) {
              const item = data.ambientes[contexts[0]?.dataIndex];
              return item
                ? `Avance: ${formatPercent(item.porcentajeAvance)}% de ${formatNumber(item.programados)}`
                : "";
            }
          }
        }
      }
    }
  });
})();
