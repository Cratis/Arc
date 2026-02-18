import{j as t}from"./jsx-runtime-Cf8x2fCZ.js";const o=({children:a,size:e="md",asCard:r=!1,className:n=""})=>{const s=[e==="full"?"story-container":e==="sm"?"story-container-sm":e==="lg"?"story-container-lg":"story-container",r?"story-card":"",n].filter(Boolean).join(" ");return t.jsx("div",{className:s,children:a})},i=({children:a,className:e=""})=>t.jsx("div",{className:`story-section ${e}`,children:a}),d=({children:a,className:e=""})=>t.jsx("div",{className:`story-grid ${e}`,children:a}),l=()=>t.jsx("hr",{className:"story-divider"}),c=({children:a,variant:e,className:r=""})=>{const n=`story-badge-${e}`;return t.jsx("span",{className:`story-badge ${n} ${r}`,children:a})};o.__docgenInfo={description:`A container component for wrapping Storybook stories with consistent spacing and styling.
Automatically adapts to dark/light mode using CSS variables.

@example
\`\`\`tsx
export const MyStory: Story = {
  render: () => (
    <StoryContainer>
      <h1>My Component</h1>
      <MyComponent />
    </StoryContainer>
  ),
};
\`\`\``,methods:[],displayName:"StoryContainer",props:{children:{required:!0,tsType:{name:"ReactReactNode",raw:"React.ReactNode"},description:"The content to render within the container"},size:{required:!1,tsType:{name:"union",raw:"'sm' | 'md' | 'lg' | 'full'",elements:[{name:"literal",value:"'sm'"},{name:"literal",value:"'md'"},{name:"literal",value:"'lg'"},{name:"literal",value:"'full'"}]},description:`The size variant of the container
- 'sm': 600px max width
- 'md': 1200px max width (default)
- 'lg': 1400px max width
- 'full': no max width`,defaultValue:{value:"'md'",computed:!1}},asCard:{required:!1,tsType:{name:"boolean"},description:"Whether to render the container as a card with background and border",defaultValue:{value:"false",computed:!1}},className:{required:!1,tsType:{name:"string"},description:"Additional CSS classes to apply",defaultValue:{value:"''",computed:!1}}}};i.__docgenInfo={description:"A section component for grouping related content within stories",methods:[],displayName:"StorySection",props:{children:{required:!0,tsType:{name:"ReactReactNode",raw:"React.ReactNode"},description:""},className:{required:!1,tsType:{name:"string"},description:"",defaultValue:{value:"''",computed:!1}}}};d.__docgenInfo={description:"A grid container for displaying multiple items in a responsive grid",methods:[],displayName:"StoryGrid",props:{children:{required:!0,tsType:{name:"ReactReactNode",raw:"React.ReactNode"},description:""},className:{required:!1,tsType:{name:"string"},description:"",defaultValue:{value:"''",computed:!1}}}};l.__docgenInfo={description:"A visual divider between story sections",methods:[],displayName:"StoryDivider"};c.__docgenInfo={description:"A status badge component for displaying colored labels",methods:[],displayName:"StoryBadge",props:{children:{required:!0,tsType:{name:"ReactReactNode",raw:"React.ReactNode"},description:""},variant:{required:!0,tsType:{name:"union",raw:"'success' | 'warning' | 'error' | 'info'",elements:[{name:"literal",value:"'success'"},{name:"literal",value:"'warning'"},{name:"literal",value:"'error'"},{name:"literal",value:"'info'"}]},description:""},className:{required:!1,tsType:{name:"string"},description:"",defaultValue:{value:"''",computed:!1}}}};export{o as S,l as a,i as b,c,d};
